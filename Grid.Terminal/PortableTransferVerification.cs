using System.Text.Json;
using Grid.Core;
using Grid.Persistence;
using Grid.Simulation;
namespace Grid.Terminal;

public static class PortableTransferVerification
{
    public static int Create(string destination)
    {
        var folder=Path.Combine(Path.GetTempPath(),"Grid614-source-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
        var world=Path.Combine(folder,"world.db"); var engine=new GridSimulationEngine(GridWorldFactory.CreateGenesis());
        using(var store=new WorldStore(world)) store.Create(new(engine.GetSnapshot().WorldId,"PORTABLE ACCEPTANCE TEST WORLD",DateTimeOffset.UtcNow),engine.GetSnapshot());
        var transfer=new GridTransfer(AppContext.BaseDirectory,world); var plan=transfer.Prepare(new(destination,false)); Console.WriteLine(GridTransfer.Preview(plan));
        Console.WriteLine(transfer.Execute(plan,()=>true));
        File.WriteAllText(Path.Combine(destination,"test-world.json"),JsonSerializer.Serialize(new { WorldId=engine.GetSnapshot().WorldId, Purpose="6.14 synthetic acceptance world; not the user's world" }));
        return 0;
    }
    public static int Relocate(string source,string destination)
    {
        var marker=Path.Combine(source,"test-world.json"); if(!File.Exists(marker)) throw new IOException("Only the disposable 6.14 test package may be relocated by this verifier.");
        using var doc=JsonDocument.Parse(File.ReadAllText(marker));
        using(var store=new WorldStore(Path.Combine(source,"data","world.db"))) if(store.Load().State.WorldId!=doc.RootElement.GetProperty("WorldId").GetGuid()) throw new IOException("Test-world identity mismatch.");
        var transfer=new GridTransfer(Path.Combine(source,"app"),Path.Combine(source,"data","world.db"));
        Console.WriteLine(transfer.Execute(transfer.Prepare(new(destination,true)),()=>true));return 0;
    }
    public static async Task<int> VerifyAsync()
    {
        var root=GridTransfer.PackageRoot(AppContext.BaseDirectory) ?? throw new IOException("Verification requires a copied package.");
        PortableAssets.Validate(root,true); Console.WriteLine("PASS: local runtime/model/hash inventory and base budget.");
        using(var store=new WorldStore(Path.Combine(root,"data","world.db"))) Console.WriteLine("Saved world (not modified): "+store.Load().State.WorldId);
        var profiles=await ModelProfileVerification.RunAsync();
        var broker=await McpWorkflowVerification.RunAsync();
        Console.WriteLine("Second-device/network isolation must be recorded by the operator. This command does not certify its physical host or disable networking.");
        return profiles==0 && broker==0 ? 0:1;
    }
}
