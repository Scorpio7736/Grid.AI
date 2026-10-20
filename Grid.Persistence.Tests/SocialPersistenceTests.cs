using Grid.Core;
using Grid.Simulation;
using Grid.Simulation.Social;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Xunit;
namespace Grid.Persistence.Tests;

public sealed class SocialPersistenceTests
{
    [Fact]
    public void VersionSevenWorldUpgradesWithoutInventingSocialParticipants()
    {
        var directory = Path.Combine(Path.GetTempPath(), "GridSocialUpgrade-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var path = Path.Combine(directory, "world.db");
            var engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis());
            engine.Step(); var before = engine.GetSnapshot();
            using (var store = new WorldStore(path)) store.Create(new(before.WorldId, "Old", DateTimeOffset.UtcNow), before);
            using (var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path, Pooling = false }.ToString()))
            {
                connection.Open(); using var command = connection.CreateCommand();
                command.CommandText = "DROP TABLE WorldCommunications; DROP TABLE WorldSocialState; DELETE FROM SchemaMigrations WHERE Version>=8; PRAGMA user_version=7;";
                command.ExecuteNonQuery();
            }
            using var reopened = new WorldStore(path);
            var loaded = reopened.Load();
            Assert.Equal(before.WorldId, loaded.State.WorldId); Assert.Equal(before.TickCount, loaded.State.TickCount);
            Assert.Equal(before.Programs.Count, loaded.State.Programs.Count);
            Assert.Empty(loaded.State.Social.Residents); Assert.Empty(loaded.State.Social.Pools);
            reopened.Save(loaded.Info, loaded.State);
            Assert.Empty(reopened.Load().State.Social.Residents);
        }
        finally { Directory.Delete(directory, true); }
    }

    [Fact]
    public void SocialStateRoundTripsRecoversAndRejectsInvalidOverwrite()
    {
        var directory = Path.Combine(Path.GetTempPath(), "GridSocial-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var world = new GridWorld(); var district = new GridDistrict { Name = "Community" }; world.AddDistrict(district);
            for (var i = 0; i < 2; i++) world.AddProgram(new GridProgram { Name = $"Agent {i}", Occupation = "Unassigned", Kind = ResidentKind.Agent }, district.Id);
            var engine = new GridSimulationEngine(world, TimeSpan.FromSeconds(3));
            foreach (var id in world.Programs.Keys) engine.ConfigureSocial(id, SocialPreference.Cooperative);
            for (var i = 0; i < 5; i++) engine.Step();
            var snapshot = engine.GetSnapshot();
            Assert.NotEmpty(snapshot.Social.Residents[0].Memories);
            Assert.Equal(TimeSpan.FromSeconds(3).Ticks, snapshot.Social.ProgressTicks);
            var info = new WorldInfo(snapshot.WorldId, "Social", DateTimeOffset.UtcNow, 7736, "test-v1");
            var path = Path.Combine(directory, "world.db");
            using var store = new WorldStore(path);
            store.Create(info, snapshot);
            Assert.Equal(JsonSerializer.Serialize(snapshot.Social), JsonSerializer.Serialize(store.Load().State.Social));
            var restored = GridSimulationEngine.Restore(store.Load().State);
            Assert.False(restored.IsRunning);
            Assert.Equal(snapshot.TickCount, restored.TickCount);
            var invalid = snapshot with { Social = snapshot.Social with { Pools = [snapshot.Social.Pools[0] with { Available = -1 }] } };
            Assert.ThrowsAny<Exception>(() => store.Save(info, invalid));
            Assert.Equal(JsonSerializer.Serialize(snapshot.Social), JsonSerializer.Serialize(store.Load().State.Social));
            File.WriteAllText(path, "damaged primary");
            store.Recover();
            Assert.Equal(JsonSerializer.Serialize(snapshot.Social), JsonSerializer.Serialize(store.Load().State.Social));
            restored.Step(); engine.Step();
            Assert.Equal(JsonSerializer.Serialize(engine.GetSnapshot().Social), JsonSerializer.Serialize(restored.GetSnapshot().Social));
        }
        finally { Directory.Delete(directory, true); }
    }
}

