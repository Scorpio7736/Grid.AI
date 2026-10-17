using Grid.Core;
using Grid.Simulation;
using Grid.Simulation.Editing;
using Xunit;

namespace Grid.TestSupport;

// Compiled only into test assemblies; no production auto-admin provider.
internal sealed class TestEditAuthority(Guid worldId) : IProgramEditAuthorizer
{
    private readonly object gate = new();
    private ProgramEditIdentity identity = new(Guid.NewGuid(), Guid.NewGuid(), 0, worldId, true, null, 1);
    private bool allowed = true;
    public EditSessionReference Session { get { lock (gate) return new(identity.SessionId, identity.Generation); } }
    public ProgramEditIdentity Identity { get { lock (gate) return identity; } }
    public void Revoke() { lock (gate) allowed = false; }
    public void Switch(bool master)
    {
        lock (gate) identity = identity with
        {
            Generation = identity.Generation + 1, IsMasterControl = master, AgentId = master ? null : Guid.NewGuid()
        };
    }
    public IProgramEditLease? Acquire(EditSessionReference session, Guid requestedWorld)
    {
        Monitor.Enter(gate);
        if (!allowed || session.SessionId != identity.SessionId || session.Generation != identity.Generation ||
            requestedWorld != identity.WorldInstanceId)
        {
            Monitor.Exit(gate);
            return null;
        }
        return new Lease(gate, identity);
    }
    private sealed class Lease(object gate, ProgramEditIdentity identity) : IProgramEditLease
    {
        public ProgramEditIdentity Identity { get; } = identity;
        public void Dispose() => Monitor.Exit(gate);
    }
}
internal static class TestEdit
{
    public static void Update(GridWorld world, GridProgram program, params ProgramFieldEdit[] fields)
    {
        var authority = new TestEditAuthority(world.InstanceId);
        var engine = new GridSimulationEngine(world, editAuthorizer: authority);
        var preview = engine.PrepareEdit(authority.Session, new(program.Id, fields));
        Assert.Equal(ProgramEditOutcome.Prepared, preview.Outcome);
        var result = engine.ApplyEdit(authority.Session, preview.Preview!);
        Assert.True(result.Outcome is ProgramEditOutcome.Applied or ProgramEditOutcome.NoChange);
    }
    public static GridWorld Genesis(Action<GridProgram> configure, int seed = 7736)
    {
        var source = GridWorldFactory.CreateGenesis(seed);
        var world = new GridWorld();
        foreach (var d in source.Districts.Values)
            world.AddDistrict(new() { Id = d.Id, Name = d.Name, Description = d.Description });
        foreach (var p in source.Programs.Values)
        {
            var copy = new GridProgram { Id = p.Id, Name = p.Name, Occupation = p.Occupation, Energy = p.Energy, Status = p.Status };
            configure(copy); // Initialize before world ownership.
            world.AddProgram(copy, p.CurrentDistrictId);
        }
        return world;
    }
}
