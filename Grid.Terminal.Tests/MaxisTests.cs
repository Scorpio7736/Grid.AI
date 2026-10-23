using Grid.Core;
using Grid.Persistence;
using Grid.Simulation;
using Grid.Simulation.Tasks;
using Microsoft.Data.Sqlite;
using Xunit;
using Status = Grid.Simulation.Tasks.TaskStatus;
namespace Grid.Terminal.Tests;

public sealed class MaxisTests : IDisposable
{
    private readonly string folder = Path.Combine(Path.GetTempPath(), "GridMaxis-" + Guid.NewGuid().ToString("N"));
    private readonly TerminalIdentitySession owner = new(() => "maxis-owner");
    private readonly TerminalSessionRegistry registry = new();
    private readonly GridSimulationEngine engine;
    private string Root => Path.Combine(folder,"source");
    private string Artifacts => Path.Combine(folder,"artifacts");
    public MaxisTests()
    {
        Directory.CreateDirectory(Root);
        File.WriteAllText(Path.Combine(Root,"sample.csproj"), "<Project><Target Name=\"Build\"><Message Text=\"MAXIS BUILD VERIFIED\" Importance=\"high\" /></Target><Target Name=\"VSTest\"><Message Text=\"MAXIS TEST TARGET VERIFIED\" Importance=\"high\" /></Target></Project>");
        File.WriteAllText(Path.Combine(Root,"sample.cs"),"class Before { }");
        registry.Add(owner); engine = new(GridWorldFactory.CreateGenesis(),editAuthorizer:registry); owner.Attach(engine); owner.Login();
        engine.StartAgents(owner.Reference); engine.ConfigureMaxisRuntime(Artifacts);
        engine.RegisterDevelopmentWorkspace(owner.Reference,"sample",Root,"sample.csproj");
    }
    private Task Save(CancellationToken token) { token.ThrowIfCancellationRequested(); GridSimulationEngine.ValidateSnapshot(engine.GetSnapshot()); return Task.CompletedTask; }
    private TaskRecord Propose(string action,string argument) => engine.ProposeTask(owner.Reference,engine.ResolveProgram("MAXIS").Id,"maxis."+action,argument,Guid.NewGuid().ToString("N"));
    private async Task<TaskRecord> Run(string action,string argument)
    {
        var task = Propose(action,argument); engine.ApproveTask(owner.Reference,task.Id); await engine.RunNextTaskAsync(owner.Reference,Save,default);
        return engine.GetSnapshot().Tasks.Records.Single(t=>t.Id==task.Id);
    }
    public void Dispose() { Directory.Delete(folder,true); }
    [Fact]
    public async Task InspectionWritesReportWithoutTouchingSource()
    {
        var task = await Run("inspect","sample|sample.cs"); Assert.Equal(Status.Completed,task.Status);
        Assert.Contains("Source inspected",task.Result); Assert.Equal("class Before { }",File.ReadAllText(Path.Combine(Root,"sample.cs")));
        Assert.Equal("class Before { }",File.ReadAllText(Directory.GetFiles(Artifacts,"report.txt",SearchOption.AllDirectories).Single()));
    }
    [Fact]
    public async Task SuggestionStagesOneReplacementAndPreservesOriginal()
    {
        var task = await Run("suggest","sample|sample.cs|Before|After"); Assert.Equal(Status.Completed,task.Status);
        Assert.Equal("class Before { }",File.ReadAllText(Path.Combine(Root,"sample.cs")));
        Assert.Equal("class After { }",File.ReadAllText(Directory.GetFiles(Artifacts,"sample.cs",SearchOption.AllDirectories).Single()));
        Assert.Contains("Source remains unchanged", Inspect(task));
    }
    private string Inspect(TaskRecord task)
    {
        var commands = DefaultCommands.Create(engine); TaskCommands.Register(commands,engine,owner,Save); commands.UseIdentity(owner);
        return commands.Execute("tasks inspect " + task.Id).Text;
    }
    [Fact]
    public async Task NonUniqueSuggestionFailsWithoutStagingOrChangingSource()
    {
        File.WriteAllText(Path.Combine(Root,"sample.cs"),"Before Before");
        Assert.Equal(Status.Failed,(await Run("suggest","sample|sample.cs|Before|After")).Status);
        Assert.Empty(Directory.GetFiles(Artifacts,"sample.cs",SearchOption.AllDirectories));
        Assert.Equal("Before Before",File.ReadAllText(Path.Combine(Root,"sample.cs")));
    }
    [Theory]
    [InlineData("inspect","sample|../outside.cs")]
    [InlineData("inspect","sample|C:/outside.cs")]
    [InlineData("inspect","sample|sample.cs:stream")]
    [InlineData("build","sample|--anything")]
    [InlineData("suggest","sample|sample.cs||new")]
    public void InvalidParametersAreRejectedBeforeCreatingTasks(string action,string argument)
    {
        Assert.Throws<ArgumentException>(()=>Propose(action,argument)); Assert.Empty(engine.GetSnapshot().Tasks.Records);
    }
    [Fact]
    public async Task RevocationBlocksApprovedWorkAndAliasesCannotBeRebound()
    {
        var task=Propose("inspect","sample|sample.cs"); engine.ApproveTask(owner.Reference,task.Id);
        engine.RevokeDevelopmentWorkspace(owner.Reference,"sample");
        await engine.RunNextTaskAsync(owner.Reference,Save,default);
        Assert.Equal(Status.Failed,engine.GetSnapshot().Tasks.Records.Single().Status); Assert.False(Directory.Exists(Artifacts));
        Assert.Throws<ArgumentException>(()=>engine.RegisterDevelopmentWorkspace(owner.Reference,"sample",Root,"sample.csproj"));
    }
    [Fact]
    public void AgentsCannotRegisterOrApproveAndMcpCanDelegateRealActions()
    {
        var agent=owner.CreateAttachment(); registry.Add(agent); agent.Switch(engine.ResolveProgram("MAXIS").Id.ToString());
        Assert.Throws<UnauthorizedAccessException>(()=>engine.RegisterDevelopmentWorkspace(agent.Reference,"other",Root,"sample.csproj"));
        var task=Propose("build","sample"); Assert.Throws<UnauthorizedAccessException>(()=>engine.ApproveTask(agent.Reference,task.Id));
        var plan=engine.Coordinate(owner.Reference,"MAXIS: inspect sample|sample.cs; MAXIS: build sample","real-plan");
        Assert.Equal(2,plan.TaskIds.Count); Assert.All(engine.GetSnapshot().Tasks.Records,t=>Assert.Equal(Status.AwaitingApproval,t.Status));
        Assert.Contains("REAL CODE EXECUTION",Inspect(task));
    }
    [Fact]
    public async Task ActualFixedBuildAndTestCaptureExitCodeAndOutput()
    {
        var build=await Run("build","sample"); Assert.Equal(Status.Completed,build.Status);
        Assert.Contains("MAXIS BUILD VERIFIED",string.Join("",Directory.GetFiles(Artifacts,"stdout.txt",SearchOption.AllDirectories).Select(File.ReadAllText)));
        var test=await Run("test","sample"); Assert.Equal(Status.Completed,test.Status);
        Assert.Contains("MAXIS TEST TARGET VERIFIED",string.Join("",Directory.GetFiles(Artifacts,"stdout.txt",SearchOption.AllDirectories).Select(File.ReadAllText)));
        Assert.Contains("no claim that tests were discovered",test.Result);
    }
    [Fact]
    public async Task BuildFailureIsNotReportedAsCompletion()
    {
        File.WriteAllText(Path.Combine(Root,"sample.csproj"),"<Project><Target Name=\"Build\"><Error Text=\"Expected failure\" /></Target></Project>");
        var result=await Run("build","sample"); Assert.Equal(Status.Failed,result.Status); Assert.Contains("report:",result.Result);
        Assert.Contains("Expected failure",string.Join("",Directory.GetFiles(Artifacts,"stdout.txt",SearchOption.AllDirectories).Select(File.ReadAllText)));
    }
    [Fact]
    public async Task PersistenceFencePreventsRealFileOperations()
    {
        var task=Propose("suggest","sample|sample.cs|Before|After"); engine.ApproveTask(owner.Reference,task.Id); var calls=0;
        await engine.RunNextTaskAsync(owner.Reference,_=>++calls==1 ? Task.FromException(new IOException("disk failed")) : Task.CompletedTask,default);
        Assert.False(Directory.Exists(Artifacts)); Assert.Equal("class Before { }",File.ReadAllText(Path.Combine(Root,"sample.cs")));
    }
    [Fact]
    public void WorkspaceRegistrySurvivesSqliteAndQueuedApprovalsDoNot()
    {
        var task=Propose("build","sample"); engine.ApproveTask(owner.Reference,task.Id);
        using var store = new WorldStore(Path.Combine(folder,"world.db")); store.Create(new WorldInfo(engine.GetSnapshot().WorldId,"MAXIS",DateTimeOffset.UtcNow),engine.GetSnapshot());
        var restored=GridSimulationEngine.Restore(store.Load().State).GetSnapshot();
        Assert.Equal(Root,Assert.Single(restored.Tasks.Workspaces).Root); Assert.Equal(Status.AwaitingApproval,Assert.Single(restored.Tasks.Records).Status);
    }
    [Fact]
    public void VersionFifteenWithoutWorkspacesMigrates()
    {
        using var store = new WorldStore(Path.Combine(folder,"world.db")); store.Create(new WorldInfo(engine.GetSnapshot().WorldId,"Old",DateTimeOffset.UtcNow),engine.GetSnapshot());
        using(var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource=store.DatabasePath,Pooling=false }.ToString()))
        {
            connection.Open(); using var command=connection.CreateCommand();
            command.CommandText="UPDATE WorldTasks SET StateJson=json_remove(StateJson,'$.Workspaces'); DELETE FROM SchemaMigrations WHERE Version>=16; PRAGMA user_version=15;"; command.ExecuteNonQuery();
        }
        Assert.Empty(store.Load().State.Tasks.Workspaces);
    }
    [Fact]
    public async Task RealProcessCancellationReturnsAndRetainsPartialLogs()
    {
        File.WriteAllText(Path.Combine(Root,"sample.csproj"),"<Project><Target Name=\"Build\"><Message Text=\"Starting wait\" Importance=\"high\"/><Exec Command=\"ping -n 20 127.0.0.1 &amp;gt; nul\" /></Target></Project>".Replace("&amp;gt;","&gt;"));
        var task=Propose("build","sample"); engine.ApproveTask(owner.Reference,task.Id);
        using var stop = new CancellationTokenSource(TimeSpan.FromMilliseconds(700));
        var run=engine.RunNextTaskAsync(owner.Reference,Save,stop.Token);
        await run.WaitAsync(TimeSpan.FromSeconds(15));
        Assert.Equal(Status.Cancelled,engine.GetSnapshot().Tasks.Records.Single().Status);
        Assert.Contains("Attempt artifacts",engine.GetSnapshot().Tasks.Records.Single().Result);
    }
    [Fact]
    public void ReparseWorkspaceAndMissingProjectAreRefused()
    {
        Assert.Throws<IOException>(()=>engine.RegisterDevelopmentWorkspace(owner.Reference,"missing",Root,"missing.csproj"));
        var link=Path.Combine(folder,"linked"); Directory.CreateSymbolicLink(link,Root);
        Assert.Throws<IOException>(()=>engine.RegisterDevelopmentWorkspace(owner.Reference,"linked",link,"sample.csproj"));
        Directory.Delete(link);
    }
}
