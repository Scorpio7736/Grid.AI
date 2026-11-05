using System.Text.Json;
using Grid.Core;
using Grid.Persistence;
using Grid.Simulation;
using Xunit;
namespace Grid.Terminal.Tests;
public sealed class TransferProgressTests
{
    [Fact]
    public void RemainingTimeCountsBothCopyAndVerificationAndExcludesFinalPhases()
    {
        var update=new TransferUpdate("Copying and hashing",100,100,0,1,"verify");
        Assert.Equal(10,TransferDashboard.RemainingCopySeconds(update,10));
        Assert.Equal(0,TransferDashboard.RemainingCopySeconds(update with {VerifiedBytes=100},10));
        Assert.Null(TransferDashboard.RemainingCopySeconds(update with {Phase="Validating assets"},1000));
        Assert.Null(TransferDashboard.RemainingCopySeconds(update,1));
    }
    [Fact]
    public void LauncherShowsGuiWhileSuppressingOnlyTheConsole()
    {
        var start=TransferDashboard.CreateStartInfo("a script.ps1","a status.json");
        Assert.Equal(System.Diagnostics.ProcessWindowStyle.Normal,start.WindowStyle);Assert.True(start.CreateNoWindow);Assert.False(start.UseShellExecute);
        Assert.Contains("a script.ps1",start.ArgumentList);Assert.Contains("a status.json",start.ArgumentList);
    }
    [Fact]
    public void PreviewIncludesEstimateBeforeConfirmationAndNoDuplicateQuestion()
    {
        var plan=new TransferPlan("destination",false,"app","world",new[]{new TransferFile("file","file",10_000_000_000)});
        var text=GridTransfer.Preview(plan);Assert.Contains("Estimated duration",text);Assert.Contains("rough estimate",text);Assert.DoesNotContain("Continue?",text);
        Assert.True(TransferDashboard.EstimateSeconds(plan)>TransferDashboard.EstimateSeconds(plan with { Files=[] }));
    }
    [Fact]
    public void CopyPercentageCannotClaimCompletionBeforeFinalVerification()
    {
        Assert.Equal(85,TransferDashboard.Percent(new("Copying and hashing",100,100,10,10,"file")));
        Assert.True(TransferDashboard.Percent(new("Validating assets",100,100,10,10,"verify"))<100);
        Assert.Equal(98,TransferDashboard.Percent(new("Cleanup pending",100,100,10,10,"wait",CleanupPending:true)));
        Assert.Equal(100,TransferDashboard.Percent(new("Copy complete",100,100,10,10,"done",Finished:true)));
    }
    [Fact]
    public void ActualCopyReportsChunkProgressAndAllValidationPhases()
    {
        var root=Path.Combine(Path.GetTempPath(),"GridProgressTest-"+Guid.NewGuid());Directory.CreateDirectory(root);
        try {
            var app=Path.Combine(root,"app");Directory.CreateDirectory(app);File.WriteAllBytes(Path.Combine(app,"asset.bin"),new byte[3*1024*1024]);
            Directory.CreateDirectory(Path.Combine(root,"data"));var world=Path.Combine(root,"data","world.db");var engine=new GridSimulationEngine(GridWorldFactory.CreateGenesis());
            using(var store=new WorldStore(world))store.Create(new(engine.GetSnapshot().WorldId,"test",DateTimeOffset.UtcNow),engine.GetSnapshot());
            var transfer=new GridTransfer(app,world,skipRuntimeValidation:true);var events=new List<TransferUpdate>();
            transfer.Execute(transfer.Prepare(new(Path.Combine(root,"copy"),false)),()=>true,observe:events.Add);
            Assert.Contains(events,e=>e.BytesDone>0 && e.BytesDone<e.TotalBytes);
            foreach(var phase in new[]{"Rechecking inventory","Copying and hashing","Validating save","Validating assets","Publishing","Copy complete"})Assert.Contains(events,e=>e.Phase==phase);
            Assert.True(events.Last().Finished);Assert.Equal(events.Last().TotalBytes,events.Last().BytesDone);
            Assert.Contains(events,e=>e.VerifiedBytes>0 && e.VerifiedBytes<e.TotalBytes);
            Assert.Contains(events,e=>e.Phase=="Validating save" && e.VerifiedBytes==e.TotalBytes);
        } finally {Directory.Delete(root,true);}
    }
    [Fact]
    public void DashboardStatusIsBoundedAndFinalStateBypassesThrottle()
    {
        var root=Path.Combine(Path.GetTempPath(),"GridProgressState-"+Guid.NewGuid());Directory.CreateDirectory(root);
        try {
            var file=Path.Combine(root,"status.json");var plan=new TransferPlan("dest",false,"app","world",[]);using var dashboard=new TransferDashboard(plan,file);
            for(var i=0;i<12;i++)dashboard.Report(new("Phase "+i,0,1,0,1,"work"));
            dashboard.Report(new("Done",1,1,1,1,"done",Finished:true));using var json=JsonDocument.Parse(File.ReadAllText(file));
            Assert.Equal(7,json.RootElement.GetProperty("Feed").GetArrayLength());Assert.Equal(100,json.RootElement.GetProperty("Percent").GetDouble());
        } finally {Directory.Delete(root,true);}
    }
}
