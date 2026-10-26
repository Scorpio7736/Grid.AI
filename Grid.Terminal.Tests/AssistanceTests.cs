using System.Text.Json;
using Grid.Communication;
using Grid.Core;
using Grid.Persistence;
using Grid.Simulation;
using Grid.Simulation.Tasks;
using Xunit;
using Status=Grid.Simulation.Tasks.TaskStatus;
namespace Grid.Terminal.Tests;

public sealed class AssistanceTests
{
    private const string Proposal="{\"steps\":[{\"agent\":\"VOLT\",\"capability\":\"volt.status\",\"argument\":\"current\"},{\"agent\":\"SENTINEL\",\"capability\":\"sentinel.firewall\",\"argument\":\"status\"}]}";
    private sealed class Model : ILocalInference
    {
        public string Prompt="", Reply="Check the captured deployment settings.";
        public bool Available=true;
        public bool Rejected;
        public bool Structured;
        public Task<InferenceReply> GenerateStructuredAsync(string prompt,CancellationToken token=default) { Structured=true; return GenerateAsync(prompt,token); }
        public TaskCompletionSource? Block;
        public TaskCompletionSource Entered=new(TaskCreationOptions.RunContinuationsAsynchronously);
        public async Task<InferenceReply> GenerateAsync(string prompt,CancellationToken token=default)
        { Prompt=prompt; Entered.TrySetResult(); if(Block is not null) await Block.Task.WaitAsync(token); return new(Available,Reply,"test") { ResponseRejected=Rejected }; }
    }
    private sealed class Fixture
    {
        public readonly TerminalIdentitySession Owner=new(()=>"assistance-owner");
        public readonly GridSimulationEngine Engine;
        public readonly CommandDispatcher Commands;
        public readonly ChatCommands Chat;
        public readonly Model Ai=new();
        public int Saves;
        public bool SaveFails;
        public Fixture(GridSimulationSnapshot? snapshot=null)
        {
            var registry=new TerminalSessionRegistry(); registry.Add(Owner);
            Engine=snapshot is null ? new(GridWorldFactory.CreateGenesis(),editAuthorizer:registry) : GridSimulationEngine.Restore(snapshot,editAuthorizer:registry);
            Owner.Attach(Engine); Owner.Login(); Engine.StartAgents(Owner.Reference);
            Commands=DefaultCommands.Create(Engine); Chat=ChatCommands.Register(Commands,Engine,Owner,Ai,_=> { Saves++; if(SaveFails) throw new IOException("test"); return Task.CompletedTask; }); Commands.UseIdentity(Owner);
            Commands.Execute("connect MCP -inline"); Commands.Execute("memory scope grid"); Saves=0;
        }
        public ContextDocument Document(string text="CAPTURED_DEPLOYMENT",string project="grid",string? role="MCP",bool persistent=true)
        {
            var d=new ContextDocument(Guid.NewGuid(),project,"C:\\missing\\notes.txt",new string('A',64),text.Length,DateTimeOffset.UtcNow,DateTimeOffset.UtcNow,"utf8",text,role is null ? null : Engine.ResolveProgram(role).Id);
            Engine.AddContext(Owner.Reference,d,persistent); return d;
        }
        public async Task<CommandResult> Run(string cmd) { var r=Commands.Execute(cmd); return r.BrokerOperation is {} op ? await op(default) : r; }
    }
    [Fact]
    public async Task RejectedAnswerDoesNotClaimTheServiceIsOfflineOrCreateTasks()
    {
        var f=new Fixture();f.Ai.Available=false;f.Ai.Rejected=true;
        var reply=await f.Run("assist advice how i can center a div in html");
        Assert.Contains("AI response rejected",reply.Text);Assert.DoesNotContain("Local AI unavailable",reply.Text);Assert.Empty(f.Engine.GetSnapshot().Tasks.Records);
    }
    [Theory]
    [InlineData("advice")][InlineData("analyze")]
    public async Task AdviceAndAnalysisUseOnlyPermittedCapturedProjectContextAndNeverMutateFilesOrTasks(string mode)
    {
        var f=new Fixture(); f.Document(); f.Document("OWNER_SECRET",role:null); f.Document("OTHER_AGENT",role:"MAXIS"); f.Document("OTHER_PROJECT",project:"elsewhere");
        var result=await f.Run("assist "+mode+" Explain deployment settings.");
        Assert.Contains("CAPTURED_DEPLOYMENT",f.Ai.Prompt); Assert.DoesNotContain("OWNER_SECRET",f.Ai.Prompt); Assert.DoesNotContain("OTHER_AGENT",f.Ai.Prompt); Assert.DoesNotContain("OTHER_PROJECT",f.Ai.Prompt);
        Assert.Contains("notes.txt",result.Text); Assert.Empty(f.Engine.GetSnapshot().Tasks.Records); Assert.Empty(f.Engine.GetConversation(f.Chat.ActiveConversation!.Value).Messages); Assert.Equal(0,f.Saves);
    }
    [Fact]
    public async Task CapturedContextRemainsAvailableAcrossConversationsAndPersistedRestartWithoutReopeningSource()
    {
        var f=new Fixture(); f.Document(); var folder=Path.Combine(Path.GetTempPath(),"GridAssist-"+Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
        try
        {
            using var store=new WorldStore(Path.Combine(folder,"world.db")); var snapshot=f.Engine.GetSnapshot(); store.Create(new(snapshot.WorldId,"Assist",DateTimeOffset.UtcNow),snapshot);
            var restored=new Fixture(store.Load().State); await restored.Run("assist analyze Explain deployment."); Assert.Contains("CAPTURED_DEPLOYMENT",restored.Ai.Prompt);
            restored.Commands.Execute("disconnect"); restored.Commands.Execute("connect MCP -inline"); restored.Commands.Execute("memory scope grid"); await restored.Run("assist analyze Explain deployment."); Assert.Contains("CAPTURED_DEPLOYMENT",restored.Ai.Prompt);
        }
        finally {Directory.Delete(folder,true);}
    }
    [Fact]
    public async Task TypedDraftDoesNotCreateTasksUntilSubmittedAndAllStepsRequireApproval()
    {
        var f=new Fixture(); f.Ai.Reply=Proposal;
        Assert.Contains("DRAFT ONLY",(await f.Run("assist propose Check resources and firewall status.")).Text);
        Assert.True(f.Ai.Structured); Assert.Empty(f.Engine.GetSnapshot().Tasks.Records); Assert.Empty(f.Engine.GetSnapshot().Tasks.Plans);
        Assert.Contains("await approval",f.Commands.Execute("assist submit").Text);
        var tasks=f.Engine.GetSnapshot().Tasks; Assert.Equal(2,tasks.Records.Count); Assert.Single(tasks.Plans);
        Assert.All(tasks.Records,t=> {Assert.Equal(Status.AwaitingApproval,t.Status);Assert.Equal(0,t.Attempts);Assert.Null(t.ApprovedBy);}); Assert.Equal(1,f.Saves);
        Assert.Contains("No draft",f.Commands.Execute("assist submit").Text);
    }
    [Theory]
    [InlineData("{}")]
    [InlineData("{\"steps\":[]}")]
    [InlineData("{\"steps\":[],\"steps\":[]}")]
    [InlineData("{\"steps\":[],\"execute\":true}")]
    [InlineData("{\"steps\":[{\"agent\":\"MCP\",\"capability\":\"mcp.nge\",\"argument\":\"yes\"}]}")]
    [InlineData("{\"steps\":[{\"agent\":\"VOLT\",\"capability\":\"volt.status\",\"argument\":\"current; JN: demo hacked\"}]}")]
    [InlineData("{\"steps\":[{\"agent\":\"VOLT\",\"capability\":\"maxis.build\",\"argument\":\"grid\"}]}")]
    [InlineData("{\"steps\":[{\"agent\":4,\"capability\":\"volt.status\",\"argument\":\"current\"}]}")]
    public async Task MalformedOrUnsupportedModelProposalsNeverCreatePartialTasks(string json)
    {
        var f=new Fixture();f.Ai.Reply=json; Assert.Contains("refused",(await f.Run("assist propose Do something.")).Text);
        Assert.Empty(f.Engine.GetSnapshot().Tasks.Records); Assert.Contains("No draft",f.Commands.Execute("assist submit").Text);
    }
    [Fact]
    public async Task UnknownWorkspaceFailsWholeDraftEvenAfterValidStep()
    {
        var f=new Fixture(); f.Ai.Reply=Proposal.Replace("{\"agent\":\"SENTINEL\",\"capability\":\"sentinel.firewall\",\"argument\":\"status\"}","{\"agent\":\"MAXIS\",\"capability\":\"maxis.build\",\"argument\":\"missing\"}");
        Assert.Contains("refused",(await f.Run("assist propose Check resources and build missing.")).Text); Assert.Empty(f.Engine.GetSnapshot().Tasks.Records);
    }
    [Theory]
    [InlineData("revoke")][InlineData("remove")][InlineData("pause")][InlineData("logout")][InlineData("scope")]
    public async Task ChangedScopeCancelsPendingInference(string change)
    {
        var f=new Fixture(); var doc=f.Document(); f.Ai.Block=new();
        var pending=f.Run("assist analyze Explain deployment."); await f.Ai.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        switch(change)
        {
            case "revoke": f.Engine.ChangeContext(f.Owner.Reference,doc.Id,false);break;
            case "remove": f.Engine.ChangeContext(f.Owner.Reference,doc.Id,true);break;
            case "pause": f.Engine.Pause();break;
            case "logout": f.Owner.Logout();break;
            default: f.Commands.Execute("memory scope another");break;
        }
        var result=await pending.WaitAsync(TimeSpan.FromSeconds(5));Assert.Contains("cancelled",result.Text,StringComparison.OrdinalIgnoreCase);Assert.Empty(f.Engine.GetSnapshot().Tasks.Records);
    }
    [Fact]
    public async Task RevocationAfterPreviewBlocksSubmissionAndNoContextResponseEntersRecall()
    {
        var f=new Fixture(); var doc=f.Document(); f.Ai.Reply=Proposal;await f.Run("assist propose Check resources."); f.Engine.ChangeContext(f.Owner.Reference,doc.Id,false);
        Assert.Contains("scope changed",f.Commands.Execute("assist submit").Text); Assert.Empty(f.Engine.GetSnapshot().Tasks.Records);
        Assert.Empty(f.Engine.GetConversation(f.Chat.ActiveConversation!.Value).Messages);
    }
    [Fact]
    public async Task SaveFailureRetryIsIdempotentAndNeverApprovesTasks()
    {
        var f=new Fixture(); f.Ai.Reply=Proposal; await f.Run("assist propose Check resources."); f.SaveFails=true;
        Assert.Contains("Save failed",f.Commands.Execute("assist submit").Text); Assert.Equal(2,f.Engine.GetSnapshot().Tasks.Records.Count);
        f.SaveFails=false; f.Commands.Execute("assist submit"); Assert.Equal(2,f.Engine.GetSnapshot().Tasks.Records.Count);
        Assert.All(f.Engine.GetSnapshot().Tasks.Records,t=>Assert.Equal(Status.AwaitingApproval,t.Status));
    }
    [Fact]
    public async Task UnavailableInferenceAndDiscardNeverCreateTasks()
    {
        var f=new Fixture(); f.Ai.Available=false;Assert.Contains("unavailable",(await f.Run("assist propose Check resources.")).Text);
        f.Ai.Available=true;f.Ai.Reply=Proposal;await f.Run("assist propose Check resources.");f.Commands.Execute("assist discard");Assert.Contains("No draft",f.Commands.Execute("assist submit").Text);Assert.Empty(f.Engine.GetSnapshot().Tasks.Records);
    }
    [Fact]
    public async Task MeetingAndProgramIdentitiesCannotBorrowPrivateAssistanceScope()
    {
        var f=new Fixture();f.Commands.Execute("disconnect");f.Commands.Execute("agent meetings MCP MAXIS -inline");Assert.Contains("one-to-one",(await f.Run("assist analyze Explain deployment.")).Text);
        f.Owner.Switch(f.Engine.ResolveProgram("MAXIS").Id.ToString());Assert.Contains("Access denied",f.Commands.Execute("assist propose Check resources.").Text);
    }
}
