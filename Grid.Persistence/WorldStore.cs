using System.Globalization;
using Grid.Core;
using Grid.Simulation;
using Microsoft.Data.Sqlite;

namespace Grid.Persistence;

public sealed record WorldInfo(Guid Id, string Name, DateTimeOffset CreatedAtUtc, int? GenerationSeed = null, string? GeneratorVersion = null);
public sealed record SavedWorld(WorldInfo Info, GridSimulationSnapshot State, DateTimeOffset SavedAtUtc);

/// <summary>One world per file. Holds an exclusive GRID session lease until disposed.</summary>
public sealed partial class WorldStore : IDisposable
{
    private readonly FileStream lease;
    private readonly object gate = new();
    private readonly Action<SqliteConnection>? configureWriteConnection;
    private bool disposed;
    public string DatabasePath { get; }
    public string BackupPath => DatabasePath + ".backup";
    private string RecoveryMarker => DatabasePath + ".recovering";

    public WorldStore(string path) : this(path, null) { }

    // Internal connection configuration seam for SQLite capacity/error verification; not a terminal capability.
    internal WorldStore(string path, Action<SqliteConnection>? configureWriteConnection)
    {
        this.configureWriteConnection = configureWriteConnection;
        DatabasePath = new SqliteGridDatabase(path).DatabasePath;
        lease = new FileStream(DatabasePath + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
    }

    public SavedWorld Load()
    {
        lock (gate) { ObjectDisposedException.ThrowIf(disposed, this); EnsureNotRecovering(); return Read(DatabasePath); }
    }

    public void EnsureCanCreate()
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            EnsureNotRecovering();
            if (File.Exists(DatabasePath) || File.Exists(BackupPath) ||
                new[] { "-journal", "-wal", "-shm" }.Any(s => File.Exists(DatabasePath + s)))
                throw new IOException("Existing world artifacts found. Load or explicitly recover; do not regenerate.");
        }
    }

    public void Create(WorldInfo info, GridSimulationSnapshot state, CancellationToken token = default)
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            EnsureNotRecovering();
            if (File.Exists(DatabasePath) || File.Exists(BackupPath) ||
                new[] { "-journal", "-wal", "-shm" }.Any(s => File.Exists(DatabasePath + s)))
                throw new IOException("Existing world artifacts found. Load or explicitly recover; do not regenerate.");
            state = Freeze(state);
            Validate(info, state);
            token.ThrowIfCancellationRequested();
            new SqliteGridDatabase(DatabasePath).Initialize();
            Write(info, state, token);
        }
    }

    public void Save(WorldInfo info, GridSimulationSnapshot state, CancellationToken token = default,
        IReadOnlyList<GridHistoryEvent>? events = null)
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            EnsureNotRecovering();
            state = Freeze(state);
            Validate(info, state);
            var previous = Read(DatabasePath);
            if (previous.Info != info) throw new InvalidDataException("World metadata does not match the existing world.");
            if (previous.State.City?.Json != state.City?.Json) throw new InvalidDataException("World creation provenance/layout cannot be replaced by an ordinary save.");
            Write(info, state, token, events);
        }
    }

    private static void Validate(WorldInfo info, GridSimulationSnapshot state)
    {
        GridSimulationEngine.ValidateSnapshot(state);
        if (info.Id != state.WorldId || string.IsNullOrWhiteSpace(info.Name) || info.CreatedAtUtc.Offset != TimeSpan.Zero)
            throw new InvalidDataException("Invalid world metadata.");
        if (state.City is { } city && (info.GenerationSeed != city.Read().Seed || info.GeneratorVersion != city.Read().GeneratorVersion))
            throw new InvalidDataException("City provenance and world metadata disagree.");
        if (info.GeneratorVersion == Grid.Core.Generation.HardwareCityGenerator.Version && state.City is null)
            throw new InvalidDataException("Generated world is missing its saved layout.");
    }

    // Public callers may supply mutable collection implementations; keep a detached copy for the write.
    private static GridSimulationSnapshot Freeze(GridSimulationSnapshot state) =>
        GridSimulationEngine.Restore(state).GetSnapshot() with { IsRunning = state.IsRunning };

    private void Write(WorldInfo info, GridSimulationSnapshot state, CancellationToken token,
        IReadOnlyList<GridHistoryEvent>? events = null)
    {
        state = PersistentState(state);
        using (var connection = OpenForWrite())
        using (var transaction = connection.BeginTransaction())
        {
            void Execute(string sql, params object?[] values)
            {
                token.ThrowIfCancellationRequested();
                using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = sql;
                for (var i = 0; i < values.Length; i++) command.Parameters.AddWithValue("$p" + i, values[i] ?? DBNull.Value);
                command.ExecuteNonQuery();
            }
            // All child replacement and clock changes commit together, including on cancellation or SQL failure.
            Execute("DELETE FROM Programs; DELETE FROM DistrictResources; DELETE FROM SimulationStates; DELETE FROM Districts; DELETE FROM Worlds;");
            var world = info.Id.ToString("D");
            Execute("INSERT INTO Worlds VALUES ($p0,$p1,$p2,$p3,$p4)", world, info.Name, Utc(info.CreatedAtUtc), info.GenerationSeed, info.GeneratorVersion);
            if (state.City is { } city)
                Execute("INSERT INTO WorldGeneration VALUES ($p0,$p1)", world, city.Json);
            foreach (var app in state.ApplicationCatalog)
                Execute("INSERT INTO ApplicationCatalog VALUES ($p0,$p1,$p2,$p3,$p4)", world, app.Key, app.Name, app.Source, Utc(app.LastSeenUtc));
            foreach (var d in state.Districts)
            {
                Execute("INSERT INTO Districts VALUES ($p0,$p1,$p2,$p3)", world, d.Id.ToString("D"), d.Name, d.Description);
                foreach (var r in d.Resources)
                    Execute("INSERT INTO DistrictResources VALUES ($p0,$p1,$p2,$p3)", world, d.Id.ToString("D"), r.Key, r.Value);
            }
            foreach (var p in state.Programs)
                Execute("INSERT INTO Programs VALUES ($p0,$p1,$p2,$p3,$p4,$p5,$p6,$p7,$p8)", world, p.Id.ToString("D"), p.Name,
                    p.Occupation, p.Energy, p.Status.ToString(), p.DistrictId.ToString("D"), p.Revision, p.Kind.ToString());
            foreach (var p in state.Programs.Where(p => p.Behavior is not null))
                Execute("INSERT INTO ResidentBehaviors VALUES ($p0,$p1,$p2)", world, p.Id.ToString("D"),
                    System.Text.Json.JsonSerializer.Serialize(p.Behavior));
            Execute("INSERT INTO SimulationStates VALUES ($p0,$p1,$p2,$p3,$p4,$p5)", world, state.TickCount,
                state.ElapsedSimulationTime.Ticks, state.TickDuration.Ticks, state.IsRunning ? 1 : 0, Utc(DateTimeOffset.UtcNow));
            foreach (var item in events ?? [])
            {
                if (item.Id == Guid.Empty || item.Tick < 0 || item.Tick > state.TickCount ||
                    item.SimulationTime < TimeSpan.Zero || item.SimulationTime > state.ElapsedSimulationTime ||
                    item.RecordedAtUtc.Offset != TimeSpan.Zero || string.IsNullOrWhiteSpace(item.Kind))
                    throw new InvalidDataException("Invalid history event.");
                Execute("""
                    INSERT INTO WorldEvents VALUES ($p0,$p1,$p2,$p3,$p4,$p5,$p6,$p7,$p8,$p9,$p10)
                    ON CONFLICT(EventId) DO NOTHING
                    """, item.Id.ToString("D"), world, Utc(item.RecordedAtUtc), item.Tick, item.SimulationTime.Ticks,
                    item.Kind, item.Severity, item.EntityId?.ToString("D"), item.EntityName, item.Detail, item.ContextJson);
            }
            token.ThrowIfCancellationRequested();
            transaction.Commit();
        }
        try { CopyValidated(DatabasePath, BackupPath); }
        catch (Exception ex) { throw new IOException("World save committed, but backup refresh failed. The previous backup was retained.", ex); }
    }

    private SqliteConnection OpenForWrite()
    {
        var connection = new SqliteGridDatabase(DatabasePath).OpenExisting();
        try { configureWriteConnection?.Invoke(connection); return connection; }
        catch { connection.Dispose(); throw; }
    }

    /// <summary>Explicit recovery only. Preserves damaged primary and SQLite sidecars for inspection.</summary>
    public void Recover()
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (nullificationStarted || File.Exists(NullificationMarker)) throw new IOException("NGE interruption requires manual review; recovery is blocked.");
            var staged = DatabasePath + ".recover-" + Guid.NewGuid().ToString("N");
            CopyValidated(BackupPath, staged);
            try
            {
                var archive = ".damaged-" + DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture) + "-" + Guid.NewGuid().ToString("N");
                File.WriteAllText(RecoveryMarker, archive);
                // Move the main file last so interruption cannot silently create a new world.
                foreach (var suffix in new[] { "-wal", "-shm", "-journal", "" })
                    if (File.Exists(DatabasePath + suffix)) File.Move(DatabasePath + suffix, DatabasePath + archive + suffix);
                File.Move(staged, DatabasePath);
                File.Delete(RecoveryMarker);
            }
            finally { if (File.Exists(staged)) File.Delete(staged); }
        }
    }

    private static void CopyValidated(string source, string destination)
    {
        _ = Read(source);
        var temp = destination + ".tmp-" + Guid.NewGuid().ToString("N");
        try
        {
            using (var from = new SqliteGridDatabase(source).OpenExisting())
            using (var to = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = temp, Pooling = false }.ToString()))
            {
                to.Open();
                from.BackupDatabase(to);
            }
            _ = Read(temp);
            if (File.Exists(destination)) File.Replace(temp, destination, null);
            else File.Move(temp, destination);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }

    private static SavedWorld Read(string path)
    {
        using var connection = new SqliteGridDatabase(path).OpenExisting();
        using var transaction = connection.BeginTransaction();
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "PRAGMA integrity_check;";
        if (!string.Equals(command.ExecuteScalar() as string, "ok", StringComparison.Ordinal))
            throw new InvalidDataException("Database integrity check failed.");
        WorldInfo info;
        command.CommandText = "SELECT WorldId,Name,CreatedAtUtc,GenerationSeed,GeneratorVersion FROM Worlds";
        using (var r = command.ExecuteReader())
        {
            if (!r.Read()) throw new InvalidDataException("The database has no saved world.");
            info = new(Id(r.GetString(0)), r.GetString(1), ParseUtc(r.GetString(2)), r.IsDBNull(3) ? null : r.GetInt32(3), r.IsDBNull(4) ? null : r.GetString(4));
            if (r.Read()) throw new InvalidDataException("This runtime requires one world per database.");
        }
        var programs = new List<GridProgramSnapshot>();
        command.CommandText = "SELECT ProgramId,Name,Occupation,Energy,CurrentDistrictId,Status,Revision,ResidentKind FROM Programs";
        using (var r = command.ExecuteReader())
            while (r.Read())
            {
                var status = r.GetString(5);
                if (!Enum.TryParse<GridProgramStatus>(status, out var parsed) || parsed.ToString() != status)
                    throw new InvalidDataException("Invalid program status.");
                programs.Add(new(Id(r.GetString(0)), r.GetString(1), r.GetString(2), r.GetFloat(3), Id(r.GetString(4)), parsed, r.GetInt64(6)) { Kind = Enum.Parse<ResidentKind>(r.GetString(7)) });
            }
        command.CommandText = "SELECT ProgramId,BehaviorJson FROM ResidentBehaviors";
        using (var r = command.ExecuteReader())
            while (r.Read())
            {
                var id = Id(r.GetString(0)); var index = programs.FindIndex(p => p.Id == id);
                if (index < 0) throw new InvalidDataException("Orphan resident behavior.");
                var behavior = System.Text.Json.JsonSerializer.Deserialize<Grid.Simulation.Behavior.ResidentBehavior>(r.GetString(1))
                    ?? throw new InvalidDataException("Missing resident behavior.");
                programs[index] = programs[index] with { Behavior = behavior };
            }
        var resources = new Dictionary<Guid, Dictionary<string, float>>();
        command.CommandText = "SELECT DistrictId,ResourceKey,Amount FROM DistrictResources";
        using (var r = command.ExecuteReader())
            while (r.Read())
            {
                var id = Id(r.GetString(0));
                if (!resources.TryGetValue(id, out var items)) resources[id] = items = new();
                items.Add(r.GetString(1), r.GetFloat(2));
            }
        var districts = new List<GridDistrictSnapshot>();
        command.CommandText = "SELECT DistrictId,Name,Description FROM Districts";
        using (var r = command.ExecuteReader())
            while (r.Read())
            {
                var id = Id(r.GetString(0));
                districts.Add(new(id, r.GetString(1), r.GetString(2), Array.AsReadOnly(programs.Where(p => p.DistrictId == id).Select(p => p.Id).ToArray()),
                    new System.Collections.ObjectModel.ReadOnlyDictionary<string, float>(resources.GetValueOrDefault(id) ?? new())));
            }
        GridSimulationSnapshot state;
        DateTimeOffset savedAt;
        command.CommandText = "SELECT TickCount,ElapsedTicks,TickDurationTicks,WasRunning,SavedAtUtc FROM SimulationStates";
        using (var r = command.ExecuteReader())
        {
            if (!r.Read()) throw new InvalidDataException("Missing simulation state.");
            var running = r.GetInt64(3);
            if (running is not (0 or 1)) throw new InvalidDataException("Invalid clock running flag.");
            state = new(r.GetInt64(0), TimeSpan.FromTicks(r.GetInt64(1)), TimeSpan.FromTicks(r.GetInt64(2)), running == 1,
                programs.AsReadOnly(), districts.AsReadOnly()) { WorldId = info.Id };
            savedAt = ParseUtc(r.GetString(4));
            if (r.Read()) throw new InvalidDataException("Multiple simulation states.");
        }
        command.CommandText = "SELECT BlueprintJson FROM WorldGeneration WHERE WorldId=$world";
        command.Parameters.AddWithValue("$world", info.Id.ToString("D"));
        if (command.ExecuteScalar() is string blueprint)
            state = state with { City = Grid.Core.Generation.CityBlueprint.Parse(blueprint) };
        Validate(info, state);
        command.CommandText = "SELECT ApplicationKey,Name,Source,LastSeenUtc FROM ApplicationCatalog WHERE WorldId=$catalogWorld";
        command.Parameters.Clear();
        command.Parameters.AddWithValue("$catalogWorld", info.Id.ToString("D"));
        var catalog = new List<KnownApplication>();
        using (var reader = command.ExecuteReader())
            while (reader.Read()) catalog.Add(new(reader.GetString(0), reader.GetString(1), reader.GetString(2),
                ParseUtc(reader.GetString(3))));
        state = state with { ApplicationCatalog = catalog.AsReadOnly() };
        Validate(info, state);
        transaction.Commit();
        return new(info, state, savedAt);
    }

    private static Guid Id(string text) => Guid.TryParseExact(text, "D", out var id) && id != Guid.Empty && text == id.ToString("D")
        ? id : throw new InvalidDataException("Invalid canonical identity.");
    private static string Utc(DateTimeOffset value) => value.ToString("O", CultureInfo.InvariantCulture);
    private static DateTimeOffset ParseUtc(string text) => DateTimeOffset.TryParseExact(text, "O", CultureInfo.InvariantCulture,
        DateTimeStyles.None, out var value) && value.Offset == TimeSpan.Zero ? value : throw new InvalidDataException("Invalid UTC timestamp.");
    private void EnsureNotRecovering()
    {
        if (nullificationStarted || File.Exists(NullificationMarker))
            throw new IOException("World nullification started. Startup/save/recovery is blocked; inspect the .nullifying marker and remaining files before explicit cleanup.");
        if (File.Exists(RecoveryMarker)) throw new IOException("An interrupted recovery must be completed with --recover.");
    }
    public void Dispose() { lock (gate) { if (disposed) return; disposed = true; lease.Dispose(); } }
}
