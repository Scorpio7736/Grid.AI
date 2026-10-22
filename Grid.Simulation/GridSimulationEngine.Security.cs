using Grid.Simulation.Tasks;
namespace Grid.Simulation;
public sealed partial class GridSimulationEngine
{
    private ISentinelProbe sentinelProbe = new WindowsSentinelProbe();
    // Trusted host/test injection; user commands cannot replace the provider.
    public void ConfigureSentinelProbe(ISentinelProbe probe)
    {
        ArgumentNullException.ThrowIfNull(probe); lock(gate) { if(taskExecuting) throw new InvalidOperationException("Cannot replace a running provider."); sentinelProbe=probe; }
    }
    private async Task<string> ReadSecurityAsync(string capability,CancellationToken token)
    {
        var json=await sentinelProbe.ReadAsync(capability,token);
        token.ThrowIfCancellationRequested();
        try { return SentinelPolicy.Render(capability,json,timeProvider.GetUtcNow()); }
        catch(System.Text.Json.JsonException) { throw new InvalidDataException("Invalid Windows security provider response; protection state unknown."); }
    }
}
