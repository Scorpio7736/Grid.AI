using System.Diagnostics;
using Grid.Core;
using Grid.Simulation;
using Grid.Simulation.Resources;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Grid.Persistence.Tests;

public sealed class PhaseTwoVerificationTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "GridPhaseTwo-" + Guid.NewGuid().ToString("N"));
    private string DatabasePath => Path.Combine(directory, "world.db");
    public PhaseTwoVerificationTests() => Directory.CreateDirectory(directory);
    public void Dispose() { foreach (var file in Directory.GetFiles(directory)) File.Delete(file); Directory.Delete(directory); }

    private static string BuiltAssembly(string project)
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "Grid7736.slnx"))) root = root.Parent;
        if (root is null) throw new DirectoryNotFoundException("Repository root not found.");
        var configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
        return Path.Combine(root.FullName, project, "bin", configuration, "net10.0", Path.GetFileName(project) + ".dll");
    }

    private static Process Start(string assembly, params string[] arguments)
    {
        var start = new ProcessStartInfo("dotnet") { UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true };
        start.ArgumentList.Add(assembly);
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        return Process.Start(start) ?? throw new InvalidOperationException("Could not start isolated process.");
    }

    private static async Task Stop(Process process)
    {
        if (!process.HasExited) process.Kill(entireProcessTree: true);
        await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
    }

    private async Task<(int Code, string Output, string Error)> Terminal(string input, params string[] extra)
    {
        using var process = Start(BuiltAssembly("Grid.Terminal"), ["--world", DatabasePath, .. extra]);
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        try
        {
            await process.StandardInput.WriteAsync("login master\n" + input);
            process.StandardInput.Close();
            await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(30));
            return (process.ExitCode, await output, await error);
        }
        finally { await Stop(process); }
    }

    [Fact]
    public async Task KilledWriterRecoversHotJournalAndPreservesCommittedWorldAndHistory()
    {
        var engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis());
        var info = new WorldInfo(engine.GetSnapshot().WorldId, "Crash fixture", DateTimeOffset.UtcNow);
        using (var store = new WorldStore(DatabasePath))
        {
            store.Create(info, engine.GetSnapshot()); engine.Step(); engine.Start(); engine.Pause();
            await new WorldPersistenceSession(store, info, engine).SaveAsync();
        }
        var backupBefore = File.ReadAllBytes(DatabasePath + ".backup");
        var before = engine.GetSnapshot();
        using (var child = Start(BuiltAssembly("TestSupport/Grid.Persistence.Probe"), DatabasePath))
        {
            var errors = child.StandardError.ReadToEndAsync();
            try
            {
                var ready = await child.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(30));
                Assert.Equal("UNCOMMITTED_WRITE_READY", ready);
                Assert.True(new FileInfo(DatabasePath + "-journal").Length > 512);
            }
            finally { await Stop(child); }
            Assert.Equal("", await errors);
        }
        using var recovered = new WorldStore(DatabasePath);
        var saved = recovered.Load();
        Assert.Equal(info, saved.Info); AssertState(before, saved.State);
        Assert.Equal(2, recovered.QueryHistory().Count);
        Assert.Equal(backupBefore, File.ReadAllBytes(recovered.BackupPath));
        using var connection = new SqliteGridDatabase(DatabasePath).OpenExisting();
        using var query = connection.CreateCommand();
        query.CommandText = "SELECT COUNT(*) FROM sqlite_schema WHERE name='InterruptedWrite'";
        Assert.Equal(0L, query.ExecuteScalar());
        query.CommandText = "PRAGMA integrity_check"; Assert.Equal("ok", query.ExecuteScalar());
    }

    [Fact]
    public async Task HeadlessTerminalSaveCloseReopenAndBackupRecoveryRetainWorld()
    {
        var first = await Terminal("step\nstep\nstart\npause\nexit\n");
        Assert.Equal(0, first.Code); Assert.Contains("World saved.", first.Output);
        SavedWorld saved;
        using (var store = new WorldStore(DatabasePath)) saved = store.Load();
        var second = await Terminal("status\nhistory kind=Clock\nexit\n");
        Assert.Equal(0, second.Code); Assert.Contains(saved.Info.Id.ToString(), second.Output);
        Assert.Contains("State: Paused", second.Output); Assert.Contains($"at tick {saved.State.TickCount}", second.Output);
        Assert.Contains("Information | Clock", second.Output);
        using (var store = new WorldStore(DatabasePath)) AssertState(saved.State, store.Load().State);
        File.WriteAllText(DatabasePath, "injected corruption");
        var invalid = await Terminal("exit\n");
        Assert.Equal(1, invalid.Code); Assert.Equal("injected corruption", File.ReadAllText(DatabasePath));
        var recovery = await Terminal("status\nexit\n", "--recover");
        Assert.Equal(0, recovery.Code);
        using var restored = new WorldStore(DatabasePath);
        AssertState(saved.State, restored.Load().State);
        Assert.Contains(Directory.GetFiles(directory, "*.damaged-*"), f => File.ReadAllText(f) == "injected corruption");
    }

    [Fact]
    public void FullyPopulatedV1WorldMigratesAndRestoresWithoutChangingIdentityOrValues()
    {
        const string world = "11111111-1111-1111-1111-111111111111";
        const string district = "22222222-2222-2222-2222-222222222222";
        const string program = "33333333-3333-3333-3333-333333333333";
        using (var c = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = DatabasePath, Pooling = false }.ToString()))
        {
            c.Open(); GridSchema.Migrate(c, [GridSchema.Migrations[0]]);
            using var command = c.CreateCommand();
            command.CommandText = $"""
                INSERT INTO Worlds VALUES ('{world}','Legacy','2020-01-01T00:00:00.0000000+00:00',7736,'legacy-v1');
                INSERT INTO Districts VALUES ('{world}','{district}','Archive','Legacy district');
                INSERT INTO Programs (WorldId,ProgramId,Name,Occupation,Energy,Status,CurrentDistrictId,Revision) VALUES ('{world}','{program}','Resident','Archivist',42.5,'Working','{district}',7);
                INSERT INTO DistrictResources VALUES ('{world}','{district}','Data',12.25);
                INSERT INTO SimulationStates VALUES ('{world}',12,30000000,2500000,1,'2020-01-01T00:00:00.0000000+00:00');
                """;
            command.ExecuteNonQuery();
        }
        using var store = new WorldStore(DatabasePath);
        var saved = store.Load(); var restored = GridSimulationEngine.Restore(saved.State);
        Assert.Equal(Guid.Parse(world), saved.Info.Id); Assert.Equal(7736, saved.Info.GenerationSeed);
        var resident = Assert.Single(restored.GetSnapshot().Programs);
        Assert.Equal(Guid.Parse(program), resident.Id); Assert.Equal(7, resident.Revision); Assert.Equal(42.5f, resident.Energy);
        Assert.Equal(GridProgramStatus.Working, resident.Status); Assert.Equal(Guid.Parse(district), resident.DistrictId);
        var area = Assert.Single(restored.GetSnapshot().Districts);
        Assert.Equal("Legacy district", area.Description); Assert.Equal(12.25f, area.Resources["Data"]);
        Assert.Equal(resident.Id, Assert.Single(area.ProgramIds)); Assert.Equal(12, restored.TickCount);
        Assert.Equal(TimeSpan.FromSeconds(3), restored.ElapsedSimulationTime); Assert.False(restored.IsRunning);
        Assert.Empty(store.QueryHistory());
        store.Save(saved.Info, restored.GetSnapshot());
        store.Recover(); AssertState(restored.GetSnapshot(), store.Load().State);
    }

    [Fact]
    public void OfflineYearsDoNotAdvanceClockOrRestoreLiveTelemetryAndSessionIdentity()
    {
        var world = GridWorldFactory.CreateGenesis(); var engine = new GridSimulationEngine(world);
        engine.Step(); engine.Step(); engine.Start();
        engine.UpdateHostResources([new(ResourceMetric.RamAvailableBytes, "host", 1024, ObservationStatus.Available,
            DateTimeOffset.UtcNow, "Fixture")]);
        var info = new WorldInfo(world.Id, "Offline", DateTimeOffset.UtcNow);
        using (var store = new WorldStore(DatabasePath)) store.Create(info, engine.GetSnapshot());
        using var reopened = new WorldStore(DatabasePath);
        var saved = reopened.Load();
        var restored = GridSimulationEngine.Restore(saved.State, new FarFutureTime());
        Assert.False(restored.IsRunning); Assert.Equal(2, restored.TickCount); Assert.Equal(TimeSpan.FromSeconds(2), restored.ElapsedSimulationTime);
        Assert.NotEqual(engine.WorldInstanceId, restored.WorldInstanceId); Assert.Equal(world.Id, restored.GetSnapshot().WorldId);
        Assert.Empty(restored.GetHostResources()); Assert.Empty(restored.CaptureHistory().Events);
        restored.Step(); Assert.Equal(3, restored.TickCount); Assert.Equal(TimeSpan.FromSeconds(3), restored.ElapsedSimulationTime);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("empty")]
    [InlineData("foreign")]
    [InlineData("future")]
    public void InvalidOrMissingWorldNeverSilentlyInitializes(string kind)
    {
        if (kind == "empty") File.WriteAllBytes(DatabasePath, []);
        if (kind == "foreign") File.WriteAllText(DatabasePath, "other application data");
        if (kind == "future")
        {
            new SqliteGridDatabase(DatabasePath).Initialize();
            using var c = new SqliteGridDatabase(DatabasePath).OpenExisting();
            using var cmd = c.CreateCommand(); cmd.CommandText = "PRAGMA user_version=999"; cmd.ExecuteNonQuery();
        }
        var before = File.Exists(DatabasePath) ? File.ReadAllBytes(DatabasePath) : null;
        using var store = new WorldStore(DatabasePath);
        Assert.ThrowsAny<Exception>(() => store.Load());
        if (before is null) Assert.False(File.Exists(DatabasePath));
        else Assert.Equal(before, File.ReadAllBytes(DatabasePath));
    }

    private static void AssertState(GridSimulationSnapshot expected, GridSimulationSnapshot actual)
    {
        Assert.Equal(expected.WorldId, actual.WorldId); Assert.Equal(expected.TickCount, actual.TickCount);
        Assert.Equal(expected.IsRunning, actual.IsRunning);
        Assert.Equal(expected.ElapsedSimulationTime, actual.ElapsedSimulationTime); Assert.Equal(expected.TickDuration, actual.TickDuration);
        Assert.Equal(expected.Programs.OrderBy(p => p.Id), actual.Programs.OrderBy(p => p.Id));
        Assert.Equal(expected.Districts.Select(d => d.Id).Order(), actual.Districts.Select(d => d.Id).Order());
        foreach (var district in expected.Districts)
        {
            var copy = actual.Districts.Single(d => d.Id == district.Id);
            Assert.Equal(district.Name, copy.Name); Assert.Equal(district.Description, copy.Description);
            Assert.Equal(district.ProgramIds.Order(), copy.ProgramIds.Order());
            Assert.Equal(district.Resources.OrderBy(r => r.Key), copy.Resources.OrderBy(r => r.Key));
        }
    }

    private sealed class FarFutureTime : TimeProvider
    { public override DateTimeOffset GetUtcNow() => new(2099, 1, 1, 0, 0, 0, TimeSpan.Zero); }
}
