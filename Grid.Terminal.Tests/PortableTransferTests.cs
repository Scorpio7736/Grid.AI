using Grid.Core;
using Grid.Core.Generation;
using Grid.Simulation;
using Grid.Simulation.Tasks;
using Xunit;
using Status=Grid.Simulation.Tasks.TaskStatus;
namespace Grid.Terminal.Tests;
public sealed class PortableTransferTests
{
    [Fact]
    public void PortablePackagesReceiveBoundedColdLoadAllowance()
    {
        Assert.Equal(TimeSpan.FromMinutes(2),PortableInferenceTiming.ColdStart(null));
        Assert.Equal(TimeSpan.FromMinutes(5),PortableInferenceTiming.ColdStart("D:\\GRID7736"));
        Assert.True(PortableInferenceTiming.AttachedTransport > PortableInferenceTiming.PortableColdStart);
        Assert.Contains("first request",PortableInferenceTiming.Notice("D:\\GRID7736"));
        Assert.Equal("",PortableInferenceTiming.Notice(null));
    }
    [Fact]
    public void CorruptSourceCannotBeResealedAsValidByCopying()
    {
        var root=Path.Combine(Path.GetTempPath(),"GridCorruptSource-"+Guid.NewGuid());Directory.CreateDirectory(root);
        try {
            var package=Path.Combine(root,"package");var app=Path.Combine(package,"app");Directory.CreateDirectory(app);
            Directory.CreateDirectory(Path.Combine(package,"data"));var world=Path.Combine(package,"data","world.db");
            var engine=new GridSimulationEngine(GridWorldFactory.CreateGenesis());
            using(var store=new Grid.Persistence.WorldStore(world))store.Create(new(engine.GetSnapshot().WorldId,"test",DateTimeOffset.UtcNow),engine.GetSnapshot());
            File.WriteAllText(Path.Combine(package,GridTransfer.Marker),"{\"version\":1}");
            var asset=Path.Combine(app,"asset.txt");File.WriteAllText(asset,"original");
            PortableAssets.WriteManifest(package,new[]{new TransferFile(asset,"app/asset.txt",8,GridTransfer.Hash(asset))});
            File.WriteAllText(asset,"tampered"); // Same length: metadata preflight cannot detect this.
            var transfer=new GridTransfer(app,world,skipRuntimeValidation:true);var destination=Path.Combine(root,"copy");
            var error=Assert.Throws<IOException>(()=>transfer.Execute(transfer.Prepare(new(destination,false)),()=>true));
            Assert.Contains("does not match",error.Message);Assert.False(Directory.Exists(destination));Assert.True(File.Exists(world));
        } finally {Directory.Delete(root,true);}
    }
    [Fact]
    public void ReceiptValidationRequiresEveryMatchingHashAndReportsFilesWithoutRereadingContent()
    {
        var root=Path.Combine(Path.GetTempPath(),"GridReceipt-"+Guid.NewGuid());Directory.CreateDirectory(root);
        try {
            var file=Path.Combine(root,"asset");File.WriteAllText(file,"verified bytes");
            var asset=new PortableAsset("asset",new FileInfo(file).Length,GridTransfer.Hash(file));
            var receipts=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase) {[file]=asset.Sha256};
            var visited=new List<string>();
            // Deny reads: receipt validation must use the completed destination verification, not rehash it.
            using(var held=new FileStream(file,FileMode.Open,FileAccess.Read,FileShare.None))
                PortableAssets.ValidateEntries(root,new[]{asset},false,receipts,visited.Add);
            Assert.Equal(new[]{"asset"},visited);
            receipts.Clear();Assert.Throws<IOException>(()=>PortableAssets.ValidateEntries(root,new[]{asset},false,receipts));
            receipts[file]=new string('0',64);Assert.Throws<IOException>(()=>PortableAssets.ValidateEntries(root,new[]{asset},false,receipts));
            receipts[file]=asset.Sha256;File.AppendAllText(file,"changed");Assert.Throws<IOException>(()=>PortableAssets.ValidateEntries(root,new[]{asset},false,receipts));
        } finally {Directory.Delete(root,true);}
    }
    [Fact]
    public void RelativeAssetInventoryRejectsEscapeMissingAndCorruptFiles()
    {
        var root=Path.Combine(Path.GetTempPath(),"Grid614Inventory-"+Guid.NewGuid());Directory.CreateDirectory(root);
        try {
            Assert.Throws<InvalidDataException>(()=>PortableAssets.ValidateEntries(root,new[]{new PortableAsset("../escape",1,"hash")},true));
            Assert.Throws<IOException>(()=>PortableAssets.ValidateEntries(root,new[]{new PortableAsset("missing",1,"hash")},true));
            var file=Path.Combine(root,"asset");File.WriteAllText(file,"abcd");var entry=new PortableAsset("asset",4,GridTransfer.Hash(file));
            PortableAssets.ValidateEntries(root,new[]{entry},true);File.WriteAllText(file,"wxyz");Assert.Throws<IOException>(()=>PortableAssets.ValidateEntries(root,new[]{entry},true));
        } finally { Directory.Delete(root,true); }
    }
    [Fact]
    public void ProductionTransferRefusesMissingRuntimeBeforeDestinationCreation()
    {
        var root=Path.Combine(Path.GetTempPath(),"Grid614Missing-"+Guid.NewGuid());Directory.CreateDirectory(root);
        var app=Path.Combine(root,"app");Directory.CreateDirectory(app);File.WriteAllText(Path.Combine(app,"Grid.Terminal.dll"),"placeholder");
        var world=Path.Combine(root,"world.db");File.WriteAllText(world,"placeholder");var dest=root+"-copy";
        try { Assert.Throws<IOException>(()=>new GridTransfer(app,world).Prepare(new(dest,false)));Assert.False(Directory.Exists(dest));Assert.True(File.Exists(world)); }
        finally { Directory.Delete(root,true); }
    }
    [Fact]
    public void DeviceAndPathChangeRevokeGrantsAndPendingApprovalWithoutChangingCity()
    {
        var state=new GridSimulationEngine(GridWorldFactory.CreateGenesis()).GetSnapshot();var agent=Guid.NewGuid();var user=Guid.NewGuid();
        var audit=new[]{new TaskAudit(DateTimeOffset.UtcNow,"Approved",user,null,"test")};
        var task=new TaskRecord(Guid.NewGuid(),"portability",agent,"volt.status","current",user,null,Status.Queued,0,user,null,audit);
        state=state with { Tasks=new(new[]{new SystemAgent(agent,AgentRole.VOLT)},new[]{task}) {
            Workspaces=new[]{new DevelopmentWorkspace("old",Path.GetTempPath(),"test.csproj",Environment.MachineName,true)},
            Storage=new(){Scopes=new[]{new StorageScope("old",Path.GetTempPath(),Environment.MachineName,true)}} } };
        var updated=PortableDevice.Reauthorize(state);
        Assert.Equal(state.WorldId,updated.WorldId);Assert.Equal(state.City,updated.City);Assert.False(updated.IsRunning);
        Assert.False(updated.Tasks.Workspaces[0].Enabled);Assert.False(updated.Tasks.Storage.Scopes[0].Enabled);
        Assert.Equal(Status.AwaitingApproval,updated.Tasks.Records[0].Status);Assert.Null(updated.Tasks.Records[0].ApprovedBy);
        Assert.Equal(2,updated.Tasks.Records[0].Audit.Count);Assert.True(state.Tasks.Workspaces[0].Enabled);
    }
    [Fact]
    public void RememberedDeviceAndLocationMatchAndOtherDevicesRemainRecorded()
    {
        var root=Path.Combine(Path.GetTempPath(),"Grid614Devices-"+Guid.NewGuid());Directory.CreateDirectory(root);
        try {
            var a=new string('A',64);var b=new string('B',64);var hardware=HardwareProfile.Unavailable(DiscoveryStatus.Unavailable,"test");
            Assert.True(PortableDevice.Changed(root,a));PortableDevice.Remember(root,a,hardware);Assert.False(PortableDevice.Changed(root,a));Assert.True(PortableDevice.Changed(root,b));
            PortableDevice.Remember(root,b,hardware);Assert.Equal(2,PortableDevice.Read(root).Count);
            var relocated=root+"-relocated";Directory.Move(root,relocated);
            try { Assert.True(PortableDevice.Changed(relocated,a)); } finally { Directory.Move(relocated,root); }
        } finally { Directory.Delete(root,true); }
    }
    [Fact]
    public void LauncherResolvesDotnetAtDestinationRatherThanSourceMachine()
    { Assert.Contains("DOTNET_ROOT=%~dp0runtime",PortableAssets.Launcher);Assert.Contains("DOTNET_ROOT_X64=%~dp0runtime",PortableAssets.Launcher);Assert.DoesNotContain(Environment.UserName,PortableAssets.Launcher); }
}
