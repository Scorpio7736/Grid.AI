namespace Grid.Simulation;

// Explicit running-world setup for historical communication/task fixtures.
// Lifecycle regression tests deliberately do not use this helper.
public static class AgentTestSetup
{
    public static Grid.Communication.Recipient CreateRunningCoordinator(this GridSimulationEngine engine)
    { engine.Start(); return engine.EnsureCoordinator(); }
    public static GridSimulationEngine PrepareChatFixture(this GridSimulationEngine engine)
    { engine.Start(); engine.EnsureCoordinator(); return engine; }
}
