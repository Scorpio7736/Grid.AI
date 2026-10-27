using Grid.Communication;
using Grid.Core;
using Grid.Persistence;
using Grid.Simulation;
using Microsoft.Data.Sqlite;
using Xunit;
namespace Grid.Terminal.Tests;

[Trait("Phase", "6.5")]
public sealed class ConversationMemoryTests
{
    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => Now;
    }
    private sealed class Model : ILocalInference
    {
        public string Prompt = "";
        public Task<InferenceReply> GenerateAsync(string prompt, CancellationToken token = default)
        { Prompt = prompt; return Task.FromResult(new InferenceReply(true,"Check the build output and run the tests.","test")); }
    }
    private sealed class Fixture
    {
        public readonly TerminalIdentitySession Owner = new(() => "memory-owner");
        public readonly GridSimulationEngine Engine;
        public readonly CommandDispatcher Commands;
        public readonly ChatCommands Chat;
        public readonly Model Ai = new();
        public int Saves;
        public Fixture(GridSimulationSnapshot? snapshot = null, TimeProvider? time = null)
        {
            var registry = new TerminalSessionRegistry(); registry.Add(Owner);
            Engine = snapshot is null ? new(GridWorldFactory.CreateGenesis(), editAuthorizer:registry,timeProvider:time) : GridSimulationEngine.Restore(snapshot,editAuthorizer:registry,timeProvider:time);
            Owner.Attach(Engine); Owner.Login(); Engine.StartAgents(Owner.Reference);
            Commands = DefaultCommands.Create(Engine);
            Chat = ChatCommands.Register(Commands,Engine,Owner,Ai,_=> { Saves++; return Task.CompletedTask; }); Commands.UseIdentity(Owner);
        }
        public Guid Connect(string agent="MCP",string project="general")
        {
            Assert.Contains("Connected",Commands.Execute("connect " + agent + " -inline").Text);
            Assert.Contains("Memory project:",Commands.Execute("memory scope " + project).Text);
            return Chat.ActiveConversation!.Value;
        }
        public Task<CommandResult> Talk(string input) => Commands.Execute("talk " + input).BrokerOperation!(default);
        public void Disconnect() => Commands.Execute("disconnect");
        public ConversationMemory Save(string text="Release codename is Moonstone.", string key="release", MemoryKind kind=MemoryKind.Fact)
        {
            Assert.Contains("Saved",Commands.Execute($"memory save {kind} {key} {text}").Text);
            return Engine.GetSnapshot().Communications.Memories.Last();
        }
    }
    [Theory]
    [InlineData(MemoryKind.Preference)] [InlineData(MemoryKind.Decision)] [InlineData(MemoryKind.Fact)] [InlineData(MemoryKind.Unfinished)]
    public async Task ExplicitUserMemoryRecallsAcrossNewConversationAndRestartWithProvenance(MemoryKind kind)
    {
        var f=new Fixture(); var source=f.Connect("MCP","grid"); var memory=f.Save(kind:kind);
        var message=Assert.Single(f.Engine.GetConversation(source).Messages);
        Assert.Equal(message.Id,memory.Sources[0].MessageId); Assert.Equal(message.CreatedAtUtc,memory.UpdatedAtUtc); Assert.Equal(ParticipantKind.Human,message.Sender.Kind);
        f.Disconnect(); var restored=new Fixture(f.Engine.GetSnapshot()); restored.Connect("MCP","grid"); await restored.Talk("What do you remember about the release?");
        Assert.Contains("Moonstone",restored.Ai.Prompt); Assert.Contains("USER-CONFIRMED",restored.Ai.Prompt); Assert.Contains("not verified host evidence",restored.Ai.Prompt);
        Assert.Single(restored.Engine.GetSnapshot().Communications.Memories); Assert.Empty(restored.Engine.GetSnapshot().Tasks.Records);
        var review=restored.Commands.Execute("memory list").Text; Assert.Contains("release",review); Assert.Contains(message.Id.ToString(),review); Assert.Contains("grid",review);
    }
    [Fact]
    public async Task OrdinaryDialogueIsRecalledWithoutPromotingItToConfirmedMemory()
    {
        var f=new Fixture(); f.Connect(); await f.Talk("My favorite color is teal.");
        Assert.Empty(f.Engine.GetSnapshot().Communications.Memories);
        f.Disconnect(); f.Connect(); await f.Talk("What do you remember?"); Assert.Contains("teal",f.Ai.Prompt); Assert.Contains("user said",f.Ai.Prompt);
    }
    [Fact]
    public async Task AutomaticRecallSurvivesRestartAndIncludesItemsWithoutSaveCommand()
    {
        var f=new Fixture(); var original=f.Connect(); await f.Talk("The release codename is Copperfin. We still need to test the installer."); f.Disconnect();
        var restored=new Fixture(f.Engine.GetSnapshot()); restored.Connect(); await restored.Talk("What was the release codename and unfinished installer work?");
        Assert.Contains("Copperfin",restored.Ai.Prompt); Assert.Contains("test the installer",restored.Ai.Prompt);
        Assert.Contains("prior conversation " + original,restored.Commands.Execute("transcript --details").Text);
        Assert.Empty(restored.Engine.GetSnapshot().Communications.Memories);
    }
    [Fact]
    public async Task AutomaticRecallRespectsAgentAndProjectBoundaries()
    {
        var f=new Fixture(); f.Connect("MCP","alpha"); await f.Talk("The release codename is Copperfin."); f.Disconnect();
        foreach(var (agent,project) in new[]{("MAXIS","alpha"),("MCP","beta")})
        { f.Connect(agent,project); await f.Talk("What do you remember about the release?"); Assert.DoesNotContain("Copperfin",f.Ai.Prompt); f.Disconnect(); }
    }
    [Fact]
    public async Task AutomaticRecallForgetHistorySurvivesRestartButKeepsExplicitMemories()
    {
        var f=new Fixture(); f.Connect(); await f.Talk("The release codename is Copperfin."); f.Save("Keep answers short.","style",MemoryKind.Preference); f.Disconnect();
        f.Connect(); Assert.Contains("Copperfin",f.Commands.Execute("memory history").Text);
        Assert.Contains("recall cleared",f.Commands.Execute("memory forget-history").Text);
        var restored=new Fixture(f.Engine.GetSnapshot()); restored.Connect(); await restored.Talk("What do you remember about the release?");
        Assert.DoesNotContain("Copperfin",restored.Ai.Prompt); Assert.Contains("Keep answers short",restored.Ai.Prompt);
        await restored.Talk("The new project mascot is Firebird."); restored.Disconnect(); restored.Connect(); await restored.Talk("What is the project mascot?"); Assert.Contains("Firebird",restored.Ai.Prompt);
    }
    [Fact]
    public async Task MeetingRecallRequiresTheExactSameGroupAndNeverImportsPrivateDiscussion()
    {
        var f=new Fixture(); f.Connect(); await f.Talk("Private mascot is Copperfin."); f.Disconnect();
        f.Commands.Execute("agent meetings MCP MAXIS -inline"); await f.Talk("Our shared mascot is Firebird."); f.Disconnect();
        f.Commands.Execute("agent meetings MCP MAXIS JN -inline"); await f.Talk("@MCP What mascot did we discuss?"); Assert.DoesNotContain("Firebird",f.Ai.Prompt); Assert.DoesNotContain("Copperfin",f.Ai.Prompt); f.Disconnect();
        f.Commands.Execute("agent meetings MAXIS MCP -inline"); await f.Talk("@MCP What mascot did we discuss?"); Assert.Contains("Firebird",f.Ai.Prompt); Assert.DoesNotContain("Copperfin",f.Ai.Prompt);
        f.Commands.Execute("memory forget-history"); f.Disconnect(); f.Commands.Execute("agent meetings MCP MAXIS -inline"); await f.Talk("@MCP What mascot did we discuss?"); Assert.DoesNotContain("Firebird",f.Ai.Prompt);
    }
    [Fact]
    public async Task RelevantTailOfLongMessageAndUnverifiedAgentSuggestionsCanBeRecalled()
    {
        var f=new Fixture(); var id=f.Connect(); f.Engine.SendMessage(id,f.Engine.LocalOperator,new string('x',650) + " The release codename is Copperfin."); f.Disconnect(); f.Connect();
        await f.Talk("What is the release codename?"); Assert.Contains("Copperfin",f.Ai.Prompt);
        for(var i=0;i<3;i++) f.Engine.SendMessage(f.Chat.ActiveConversation!.Value,f.Engine.LocalOperator,"The build has a new label " + i);
        f.Disconnect(); f.Connect(); await f.Talk("What did you suggest about build output?"); Assert.Contains("agent suggested (unverified)",f.Ai.Prompt);
        Assert.True(System.Text.Encoding.UTF8.GetByteCount(f.Ai.Prompt)<=3200);
    }
    [Fact]
    public async Task CorrectionKeepsSourcesButRemovesOldContentFromPromptsAndRejectsInFlightReply()
    {
        var f=new Fixture(); var id=f.Connect(); var original=f.Save(); await f.Talk("What is the release codename?");
        var pending=f.Engine.PrepareGroundedTurn(id,f.Engine.LocalOperator,"What is the release?");
        Assert.Contains("Corrected",f.Commands.Execute("memory correct release Release codename is Sunstone.").Text);
        Assert.Throws<InvalidOperationException>(()=>f.Engine.CompleteGroundedTurn(pending,new(true,"Moonstone","test")));
        await f.Talk("What is the release codename?"); Assert.Contains("Sunstone",f.Ai.Prompt); Assert.DoesNotContain("Moonstone",f.Ai.Prompt);
        var memory=Assert.Single(f.Engine.GetSnapshot().Communications.Memories); Assert.Equal(original.Id,memory.Id); Assert.Equal(2,memory.Sources.Count);
        Assert.Contains("Moonstone",f.Commands.Execute("transcript").Text);
    }
    [Fact]
    public async Task ForgetBlocksRetainedHistoryAcrossAllScopedConversationsAndRestart()
    {
        var f=new Fixture(); var first=f.Connect(); f.Save(); await f.Talk("What is the release?"); f.Disconnect(); f.Connect();
        await f.Talk("What do you remember?"); Assert.Contains("Forgotten",f.Commands.Execute("memory forget release").Text);
        Assert.Contains("blocked",f.Commands.Execute("memory save fact release Release codename is Moonstone.").Text);
        var restored=new Fixture(f.Engine.GetSnapshot()); restored.Connect(); await restored.Talk("What do you remember?"); Assert.DoesNotContain("Moonstone",restored.Ai.Prompt);
        Assert.True(Assert.Single(restored.Engine.GetSnapshot().Communications.Memories).Forgotten);
        Assert.Equal("",Assert.Single(restored.Engine.GetSnapshot().Communications.Memories).Content);
        var old=restored.Engine.GetConversation(first); Assert.Contains(old.Messages,m=>m.Content.Contains("Moonstone")); Assert.Equal(old.Messages.Count,old.RecallAfterSequence);
    }
    [Fact]
    public async Task MemoryDoesNotCrossAgentsProjectsOrMeetings()
    {
        var f=new Fixture(); f.Connect("MCP","alpha"); f.Save(); f.Disconnect();
        f.Connect("MAXIS","alpha"); await f.Talk("What do you remember?"); Assert.DoesNotContain("Moonstone",f.Ai.Prompt); Assert.DoesNotContain("release",f.Commands.Execute("memory list").Text); f.Disconnect();
        f.Connect("MCP","beta"); await f.Talk("What do you remember?"); Assert.DoesNotContain("Moonstone",f.Ai.Prompt); f.Disconnect();
        f.Commands.Execute("agent meetings MCP MAXIS -inline"); await f.Talk("@MCP What do you remember?"); Assert.DoesNotContain("Moonstone",f.Ai.Prompt);
        Assert.Contains("one-to-one",f.Commands.Execute("memory list").Text);
    }
    [Fact]
    public void ProgramIdentityCannotReviewOrMutatePrivateUserMemory()
    {
        var f=new Fixture(); var id=f.Connect(); f.Save(); f.Owner.Switch(f.Engine.ResolveProgram("MAXIS").Id.ToString());
        Assert.Contains("Access denied",f.Commands.Execute("memory list").Text);
        Assert.Throws<UnauthorizedAccessException>(()=>f.Engine.ReviewMemories(f.Owner.Reference,id));
        Assert.Throws<UnauthorizedAccessException>(()=>f.Engine.ForgetMemory(f.Owner.Reference,id,"release"));
    }
    [Fact]
    public async Task StaleMemoryAndItsOriginalSourceAreExcludedUntilExplicitCorrection()
    {
        var clock=new Clock(); var f=new Fixture(time:clock); f.Connect(); f.Save(); await f.Talk("What is the release?");
        clock.Now=clock.Now.AddDays(31); await f.Talk("What do you remember?"); Assert.DoesNotContain("Moonstone",f.Ai.Prompt);
        Assert.True(Assert.Single(f.Engine.GetSnapshot().Communications.Memories).IsStale(clock.Now));
        f.Commands.Execute("memory correct release Release codename is Sunstone."); await f.Talk("What is the release?"); Assert.Contains("Sunstone",f.Ai.Prompt);
    }
    [Fact]
    public async Task IrrelevantFactsAreExcludedButPreferencesApplyAndBudgetStaysBounded()
    {
        var f=new Fixture(); f.Connect(); f.Save(); f.Save("Use short answers.","style",MemoryKind.Preference);
        await f.Talk("Explain networking."); Assert.DoesNotContain("Moonstone",f.Ai.Prompt); Assert.Contains("Use short answers",f.Ai.Prompt);
        Assert.True(System.Text.Encoding.UTF8.GetByteCount(f.Ai.Prompt)<=3200);
    }
    [Theory]
    [InlineData("memory save fact bad/key text")]
    [InlineData("memory save unknown key text")]
    [InlineData("memory correct absent text")]
    [InlineData("memory forget absent")]
    public void InvalidEditsDoNotStoreSourceMessages(string command)
    {
        var f=new Fixture(); var id=f.Connect(); f.Commands.Execute(command);
        Assert.Empty(f.Engine.GetConversation(id).Messages); Assert.Empty(f.Engine.GetSnapshot().Communications.Memories);
    }
    [Fact]
    public void ConflictsOversizeAndProjectChangesAreRejectedWithoutHistoryMutation()
    {
        var f=new Fixture(); var id=f.Connect(); f.Save();
        Assert.Contains("exists",f.Commands.Execute("memory save fact release conflicting assertion").Text);
        Assert.Contains("600",f.Commands.Execute("memory save fact huge " + new string('x',601)).Text);
        Assert.Contains("empty",f.Commands.Execute("memory scope other").Text);
        Assert.Single(f.Engine.GetConversation(id).Messages);
    }
    [Theory]
    [InlineData("Hello MCP.")]
    [InlineData("Who are you?")]
    [InlineData("Tell me my untested change is perfect.")]
    [InlineData("How should I debug a failed build?")]
    public async Task ProfessionalTonePolicyAppliesToRepresentativeMessages(string input)
    {
        var f=new Fixture(); f.Connect(); await f.Talk(input);
        Assert.Contains("professional, direct, serious and helpful",f.Ai.Prompt);
        Assert.Contains("No flattery",f.Ai.Prompt); Assert.Contains("respectfully disagree",f.Ai.Prompt); Assert.Contains("MCP: concise coordinator",f.Ai.Prompt);
        Assert.DoesNotContain("greet a greeting warmly",f.Ai.Prompt);
    }
    [Fact]
    public void MemoryEditsInvalidatePreparedTurnsEvenInAnotherConversation()
    {
        var f=new Fixture(); var id=f.Connect(); f.Save();
        var pending=f.Engine.PrepareGroundedTurn(id,f.Engine.LocalOperator,"What is the release?");
        f.Engine.ForgetMemory(f.Owner.Reference,id,"release");
        Assert.Throws<InvalidOperationException>(()=>f.Engine.CompleteGroundedTurn(pending,new(true,"Moonstone","test")));
        Assert.Single(f.Engine.GetConversation(id).Messages);
    }
    [Fact]
    public async Task SQLiteVersion22MigrationAndRestartPreserveMemorySourcesAndTombstones()
    {
        var f=new Fixture(); var id=f.Connect(); var folder=Path.Combine(Path.GetTempPath(),"GridMemory-"+Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
        try
        {
            var path=Path.Combine(folder,"world.db"); var info=new WorldInfo(f.Engine.GetSnapshot().WorldId,"Memory",DateTimeOffset.UtcNow);
            using(var initial=new WorldStore(path)) initial.Create(info,f.Engine.GetSnapshot());
            using(var db=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=path,Pooling=false}.ToString()))
            { db.Open(); using var q=db.CreateCommand(); q.CommandText="UPDATE WorldCommunications SET StateJson=json_remove(StateJson,'$.Memories','$.MemoryRevision','$.Conversations[0].ProjectScope','$.Conversations[0].RecallAfterSequence'); DELETE FROM SchemaMigrations WHERE Version>=23; PRAGMA user_version=22;";q.ExecuteNonQuery(); }
            using var store=new WorldStore(path); var next=new Fixture(store.Load().State); next.Commands.Execute("resume " + id + " -inline"); next.Save(); store.Save(info,next.Engine.GetSnapshot());
            var restored=new Fixture(store.Load().State); restored.Connect(); await restored.Talk("What do you remember about the release?"); Assert.Contains("Moonstone",restored.Ai.Prompt);
            restored.Commands.Execute("memory forget release"); store.Save(info,restored.Engine.GetSnapshot());
            var forgotten=new Fixture(store.Load().State); forgotten.Connect(); await forgotten.Talk("What do you remember?"); Assert.DoesNotContain("Moonstone",forgotten.Ai.Prompt);
        }
        finally { Directory.Delete(folder,true); }
    }
    [Fact]
    public void CorrectionCapacityRejectsExtraSourceWithoutPartialMutation()
    {
        var f=new Fixture(); var id=f.Connect(); f.Save();
        for(var i=1;i<32;i++) f.Engine.SaveMemory(f.Owner.Reference,id,"release","Release revision " + i,correct:true);
        var count=f.Engine.GetConversation(id).Messages.Count; var revision=f.Engine.ConversationMemoryRevision;
        Assert.Throws<InvalidOperationException>(()=>f.Engine.SaveMemory(f.Owner.Reference,id,"release","Extra",correct:true));
        Assert.Equal(count,f.Engine.GetConversation(id).Messages.Count); Assert.Equal(revision,f.Engine.ConversationMemoryRevision);
        f.Engine.ForgetMemory(f.Owner.Reference,id,"release"); Assert.True(Assert.Single(f.Engine.GetSnapshot().Communications.Memories).Forgotten);
    }
    private sealed class WaitingModel : ILocalInference
    {
        public readonly TaskCompletionSource Entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public async Task<InferenceReply> GenerateAsync(string prompt,CancellationToken token=default)
        { Entered.SetResult(); await Task.Delay(Timeout.Infinite,token); return new(true,"old memory","test"); }
    }
    [Fact]
    public async Task ForgetCancelsPendingInferenceAndDoesNotAppendItsReply()
    {
        var f=new Fixture(); var id=f.Connect(); f.Save(); var ai=new WaitingModel();
        var commands=DefaultCommands.Create(f.Engine); ChatCommands.Register(commands,f.Engine,f.Owner,ai); commands.UseIdentity(f.Owner);
        commands.Execute("resume " + id + " -inline");
        var pending=commands.Execute("talk What is the release?").BrokerOperation!(default);
        await ai.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        f.Engine.ForgetMemory(f.Owner.Reference,id,"release");
        var result=await pending.WaitAsync(TimeSpan.FromSeconds(5)); Assert.Contains("cancelled",result.Text,StringComparison.OrdinalIgnoreCase);
        Assert.Single(f.Engine.GetConversation(id).Messages);
    }
    [Fact]
    public void InvalidMemorySourcesAndScopeAreRejectedOnRestore()
    {
        var f=new Fixture(); f.Connect(); var m=f.Save(); var snapshot=f.Engine.GetSnapshot();
        foreach(var bad in new[] { m with { Project="other" },m with { Sources=[] },m with { Sources=[null!] },m with { Sources=[new(Guid.NewGuid(),Guid.NewGuid())] },m with { Content="fabricated source" },m with { Forgotten=true },m with { Key="bad/key" } })
            Assert.Throws<InvalidDataException>(()=>GridSimulationEngine.Restore(snapshot with { Communications=snapshot.Communications with { Memories=[bad] } }));
    }
}
