using Grid.Simulation.Tasks;
namespace Grid.Simulation;
public sealed partial class GridSimulationEngine
{
    private ITronProbe tronProbe = new WindowsTronProbe();
    // Trusted host/test injection; user commands cannot replace the provider.
    public void ConfigureTronProbe(ITronProbe probe)
    {
        ArgumentNullException.ThrowIfNull(probe); lock(gate) { if(taskExecuting) throw new InvalidOperationException("Cannot replace a running provider."); tronProbe=probe; }
    }
    private async Task<string> ReadSecurityAsync(string capability,CancellationToken token)
    {
        var json=await tronProbe.ReadAsync(capability,token);
        token.ThrowIfCancellationRequested();
        try { return TronPolicy.Render(capability,json,timeProvider.GetUtcNow()); }
        catch(System.Text.Json.JsonException) { throw new InvalidDataException("Invalid Windows security provider response; protection state unknown."); }
    }
}
