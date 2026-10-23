using Grid.Core;
using Grid.Persistence;
using Grid.Simulation;
using Grid.Simulation.Tasks;
using Microsoft.Data.Sqlite;
using System.Text.Json;
using Xunit;
using Status=Grid.Simulation.Tasks.TaskStatus;
namespace Grid.Terminal.Tests;
public sealed class JnTests : IDisposable
{
    private readonly string folder=Path.Combine(Path.GetTempPath(),"GridJn-"+Guid.NewGuid().ToString("N"));
    private string Source=>Path.Combine(folder,"source");
    private string Database=>Path.Combine(folder,"world.db");
    private readonly TerminalSessionRegistry registry=new();
    private readonly TerminalIdentitySession owner=new(()=>"jn-owner");
    private readonly GridSimulationEngine engine;
    public JnTests()
    {
        Directory.CreateDirectory(Source); File.WriteAllText(Path.Combine(Source,"a.txt"),"duplicate"); File.WriteAllText(Path.Combine(Source,"b.txt"),"duplicate"); File.WriteAllText(Path.Combine(Source,"c.txt"),"different");
        File.WriteAllBytes(Database,new byte[100]);
        registry.Add(owner); engine=new(GridWorldFactory.CreateGenesis(),editAuthorizer:registry); owner.Attach(engine); owner.Login(); engine.StartAgents(owner.Reference);
        engine.ConfigureJnRuntime(Database,Path.Combine(folder,"content")); engine.RegisterStorageScope(owner.Reference,"sample",Source);
    }
    public void Dispose() { Directory.Delete(folder,true); }
    private Task Save(CancellationToken token) { token.ThrowIfCancellationRequested(); GridSimulationEngine.ValidateSnapshot(engine.GetSnapshot()); return Task.CompletedTask; }
    private TaskRecord Propose(string action,string argument)=>engine.ProposeTask(owner.Reference,engine.ResolveProgram("JN").Id,"jn."+action,argument,Guid.NewGuid().ToString("N"));
    private async Task<TaskRecord> Run(string action,string argument)
    {
        var task=Propose(action,argument); engine.ApproveTask(owner.Reference,task.Id); await engine.RunNextTaskAsync(owner.Reference,Save,default);
        return engine.GetSnapshot().Tasks.Records.Single(t=>t.Id==task.Id);
    }
    private JsonDocument Report(TaskRecord task)=>JsonDocument.Parse(File.ReadAllText(Path.Combine(Database+".jn-artifacts",task.Id.ToString("N")+"-1","report.json")));
    [Fact]
    public async Task InventoryAndDuplicateCleanupPreserveEverySourceFile()
    {
        var before=Directory.GetFiles(Source).ToDictionary(p=>p,File.ReadAllText);
        var inventory=await Run("inventory","sample"); Assert.Equal(Status.Completed,inventory.Status);
        using(var report=Report(inventory)) Assert.Equal(3,report.RootElement.GetProperty("files").GetArrayLength());
        var duplicates=await Run("duplicates","sample"); using(var report=Report(duplicates)) Assert.Single(report.RootElement.GetProperty("duplicateGroups").EnumerateArray());
        var cleanup=await Run("cleanup","sample"); Assert.Contains("No files deleted",cleanup.Result);
        using(var report=Report(cleanup)) Assert.Contains("PROPOSAL ONLY",report.RootElement.GetProperty("cleanup").GetString());
        Assert.Equal(before.Count,Directory.GetFiles(Source).Length); foreach(var item in before) Assert.Equal(item.Value,File.ReadAllText(item.Key));
    }
    [Fact]
    public async Task SingleFileScopeWorksAndMissingScopeFailsBeforeProposal()
    {
        engine.RegisterStorageScope(owner.Reference,"one",Path.Combine(Source,"a.txt"));
        using var report=Report(await Run("inventory","one")); Assert.Single(report.RootElement.GetProperty("files").EnumerateArray());
        Assert.Throws<ArgumentException>(()=>Propose("inventory","missing"));
    }
    [Theory]
    [InlineData("delete","sample")]
    [InlineData("inventory","../source")]
    [InlineData("quota","-5")]
    [InlineData("quota","1001")]
    public void DestructiveAndInvalidRequestsAreRejected(string action,string argument)=>Assert.Throws<ArgumentException>(()=>Propose(action,argument));
    [Fact]
    public async Task ManagedStorageCountsDatabaseBackupModelsAndAssetsNotExternalScope()
    {
        File.WriteAllBytes(Database+".backup",new byte[200]);
        var models=Path.Combine(folder,"content","models"); Directory.CreateDirectory(models); File.WriteAllBytes(Path.Combine(models,"model.bin"),new byte[300]);
        var assets=Path.Combine(folder,"content","assets"); Directory.CreateDirectory(assets); File.WriteAllBytes(Path.Combine(assets,"asset.bin"),new byte[400]);
        using var report=Report(await Run("storage","status"));
        Assert.Equal(1000,report.RootElement.GetProperty("bytes").GetInt64()); Assert.True(report.RootElement.GetProperty("complete").GetBoolean());
        Assert.Equal(20_000_000_000,engine.StorageFor(owner.Reference).QuotaBytes);
    }
    [Fact]
    public async Task ExpansionRequiresCompletedProposalAndSeparateMasterApproval()
    {
        var task=await Run("quota","25"); Assert.Equal(Status.Completed,task.Status);
        var state=engine.StorageFor(owner.Reference); Assert.Equal(StorageState.InitialQuota,state.QuotaBytes); var p=Assert.Single(state.Proposals);
        Assert.Equal(engine.ResolveProgram("MCP").Id,p.CoordinatorId); Assert.Equal(100,p.MeasuredBytes);
        var agent=owner.CreateAttachment(); registry.Add(agent); agent.Switch(engine.ResolveProgram("MCP").Id.ToString());
        Assert.Single(engine.StorageFor(agent.Reference).Proposals);
        Assert.Throws<UnauthorizedAccessException>(()=>engine.ApproveStorageQuota(agent.Reference,p.Id));
        engine.ApproveStorageQuota(owner.Reference,p.Id); Assert.Equal(25_000_000_000,engine.StorageFor(owner.Reference).QuotaBytes);
        Assert.Throws<InvalidOperationException>(()=>engine.ApproveStorageQuota(owner.Reference,p.Id));
        GridSimulationEngine.ValidateSnapshot(engine.GetSnapshot());
        Assert.Contains(engine.CaptureHistory().Events,e=>e.Kind=="QuotaEscalatedToMCP");
    }
    [Fact]
    public async Task RevokedScopeCannotExecuteAndCannotBeRebound()
    {
        var task=Propose("inventory","sample"); engine.ApproveTask(owner.Reference,task.Id); engine.RevokeStorageScope(owner.Reference,"sample");
        await engine.RunNextTaskAsync(owner.Reference,Save,default);
        Assert.Equal(Status.Failed,Assert.Single(engine.GetSnapshot().Tasks.Records).Status); Assert.False(Directory.Exists(Database+".jn-artifacts"));
        Assert.Throws<ArgumentException>(()=>engine.RegisterStorageScope(owner.Reference,"sample",Source));
    }
    [Fact]
    public void ScopeRegistrationAndQuotaHistoryAreRoleRestricted()
    {
        var agent=owner.CreateAttachment(); registry.Add(agent); agent.Switch(engine.ResolveProgram("MAXIS").Id.ToString());
        Assert.Throws<UnauthorizedAccessException>(()=>engine.RegisterStorageScope(agent.Reference,"other",Source));
        Assert.Throws<UnauthorizedAccessException>(()=>engine.StorageFor(agent.Reference)); Assert.Empty(engine.TasksFor(agent.Reference).Storage.Scopes);
        Assert.Throws<ArgumentException>(()=>engine.ProposeTask(owner.Reference,agent.CurrentRecipient.Id,"jn.inventory","sample","borrow"));
        Assert.Equal(2,engine.Coordinate(owner.Reference,"JN: inventory sample; JN: storage status","jn-plan").TaskIds.Count);
    }
    [Fact]
    public async Task PersistenceFenceAndCancellationPreventReports()
    {
        var task=Propose("inventory","sample"); engine.ApproveTask(owner.Reference,task.Id); var calls=0;
        await engine.RunNextTaskAsync(owner.Reference,_=>++calls==1 ? Task.FromException(new IOException("disk")) : Task.CompletedTask,default);
        Assert.False(Directory.Exists(Database+".jn-artifacts"));
        using var stop=new CancellationTokenSource(); stop.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>new JnRuntime(Database,folder).ExecuteAsync(task,new("sample",Source,Environment.MachineName,true),StorageState.InitialQuota,stop.Token));
    }
    [Fact]
    public async Task LinksAreNotFollowedAndLimitsProducePartialReports()
    {
        var linked=Path.Combine(Source,"linked"); Directory.CreateSymbolicLink(linked,folder);
        var task=await Run("inventory","sample"); using(var report=Report(task)) Assert.False(report.RootElement.GetProperty("complete").GetBoolean());
        Directory.Delete(linked);
        Assert.Throws<ArgumentException>(()=>engine.RegisterStorageScope(owner.Reference,"root",Path.GetPathRoot(Source)!));
        for(var i=0;i<2001;i++) File.WriteAllText(Path.Combine(Source,"entry-"+i),"");
        using var bounded=Report(await Run("inventory","sample")); Assert.False(bounded.RootElement.GetProperty("complete").GetBoolean()); Assert.True(bounded.RootElement.GetProperty("files").GetArrayLength()<=2000);
    }
    [Fact]
    public async Task LargeDuplicateCandidatesAreExplicitlySkipped()
    {
        foreach(var name in new[]{"big-a","big-b"}) { using var file=File.Create(Path.Combine(Source,name)); file.SetLength(17*1024*1024); }
        using var report=Report(await Run("duplicates","sample")); Assert.False(report.RootElement.GetProperty("complete").GetBoolean());
        Assert.Contains(report.RootElement.GetProperty("issues").EnumerateArray(),x=>x.GetString()!.Contains("hashing skipped"));
    }
    [Fact]
    public async Task SqlitePreservesQuotaDecisionsAndRejectsForgedLimit()
    {
        await Run("quota","25"); engine.ApproveStorageQuota(owner.Reference,engine.StorageFor(owner.Reference).Proposals.Single().Id);
        using var store=new WorldStore(Path.Combine(folder,"saved.db")); store.Create(new WorldInfo(engine.GetSnapshot().WorldId,"JN",DateTimeOffset.UtcNow),engine.GetSnapshot());
        var snapshot=store.Load().State; var restored=GridSimulationEngine.Restore(snapshot).GetSnapshot();
        Assert.Equal(25_000_000_000,restored.Tasks.Storage.QuotaBytes); Assert.Single(restored.Tasks.Storage.Proposals); Assert.Single(restored.Tasks.Storage.Scopes);
        var proposal=snapshot.Tasks.Storage.Proposals.Single();
        Assert.Throws<InvalidDataException>(()=>GridSimulationEngine.Restore(snapshot with { Tasks=snapshot.Tasks with { Storage=snapshot.Tasks.Storage with { Proposals=[proposal with { RequestedBytes=26_000_000_000 }] } } }));
        Assert.Throws<InvalidDataException>(()=>GridSimulationEngine.Restore(snapshot with { Tasks=snapshot.Tasks with { Storage=snapshot.Tasks.Storage with { QuotaBytes=30_000_000_000 } } }));
    }
    [Fact]
    public void VersionSeventeenMigratesWithDefaultTwentyGbQuota()
    {
        using var store=new WorldStore(Path.Combine(folder,"old.db")); store.Create(new WorldInfo(engine.GetSnapshot().WorldId,"Old",DateTimeOffset.UtcNow),engine.GetSnapshot());
        using(var connection=new SqliteConnection(new SqliteConnectionStringBuilder { DataSource=store.DatabasePath,Pooling=false }.ToString()))
        {
            connection.Open(); using var command=connection.CreateCommand(); command.CommandText="UPDATE WorldTasks SET StateJson=json_remove(StateJson,'$.Storage'); DELETE FROM SchemaMigrations WHERE Version>=18; PRAGMA user_version=17;"; command.ExecuteNonQuery();
        }
        Assert.Equal(StorageState.InitialQuota,store.Load().State.Tasks.Storage.QuotaBytes); Assert.Empty(store.Load().State.Tasks.Storage.Scopes);
    }
    [Fact]
    public async Task TerminalShowsReviewableQuotaProposalAndShortReferenceApproval()
    {
        await Run("quota","25"); var commands=DefaultCommands.Create(engine); JnCommands.Register(commands,engine,owner,Save); commands.UseIdentity(owner);
        Assert.Contains("20 decimal GB",commands.Execute("jn quota").Text); var p=engine.StorageFor(owner.Reference).Proposals.Single();
        Assert.Contains("Measured: 100",commands.Execute("jn quota inspect "+p.Id.ToString()[..8]).Text);
        Assert.Contains("saved",commands.Execute("jn quota approve "+p.Id.ToString()[..8]).Text); Assert.Contains("25 decimal GB",commands.Execute("jn quota").Text);
    }
}
