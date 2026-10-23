using System.Text;
using Grid.Communication;
using Grid.Core;
using Grid.Simulation;
using Xunit;
namespace Grid.Terminal.Tests;

public sealed class GroundedDialogueTests
{
    private sealed class Inference(Func<string,CancellationToken,Task<InferenceReply>> run) : ILocalInference
    { public Task<InferenceReply> GenerateAsync(string prompt, CancellationToken token = default) => run(prompt, token); }
    private sealed class Fixture
    {
        public readonly GridSimulationEngine Engine;
        public readonly TerminalIdentitySession Identity = new(() => "grounded-owner");
        public readonly CommandDispatcher Commands;
        public readonly ChatCommands Chat;
        public Fixture(ILocalInference ai, Func<CancellationToken,Task>? save = null)
        {
            var registry = new TerminalSessionRegistry(); registry.Add(Identity);
            Engine = new(GridWorldFactory.CreateGenesis(), editAuthorizer: registry); Identity.Attach(Engine); Identity.Login();
            Commands = DefaultCommands.Create(Engine); Chat = ChatCommands.Register(Commands, Engine, Identity, ai, save); Commands.UseIdentity(Identity);
            Commands.Execute("connect MCP");
        }
        public Task<CommandResult> Talk(string input) => Commands.Execute("talk " + input).BrokerOperation!(default);
    }
    private static ILocalInference Reply(string text = "Suggestion: inspect the current status.") => new Inference((_,_) => Task.FromResult(new InferenceReply(true,text,"test")));

    [Fact]
    public async Task ProductionChatStoresAttributedPairWithoutTasksAndPersistsOnce()
    {
        var saves = 0; var f = new Fixture(Reply(), _ => { saves++; return Task.CompletedTask; });
        var result = await f.Talk("What should we do?");
        Assert.Contains("MCP: Suggestion:", result.Text); Assert.DoesNotContain("OBSERVED FACTS", result.Text); Assert.DoesNotContain("SOURCES", result.Text);
        Assert.NotNull(result.AnimateFrom); Assert.Equal(1,saves);
        var state = f.Engine.GetSnapshot(); Assert.Empty(state.Tasks.Records);
        var restored = GridSimulationEngine.Restore(state); var messages = restored.GetConversation(f.Chat.ActiveConversation!.Value).Messages;
        Assert.Equal(2,messages.Count); Assert.Contains("SOURCES",messages[1].Content);
    }
    [Fact]
    public async Task PlainTranscriptHidesMetadataAndDetailsPreserveOriginalEvidence()
    {
        var f = new Fixture(Reply("Hello! What would you like to work on?"));
        await f.Talk("Hello MCP.");
        var plain = f.Commands.Execute("transcript").Text;
        Assert.Contains("You: Hello MCP.", plain); Assert.Contains("MCP: Hello!", plain);
        Assert.DoesNotContain("tick ", plain); Assert.DoesNotContain("SOURCES", plain);
        var detailed = f.Commands.Execute("transcript --details").Text;
        Assert.Contains("OBSERVED FACTS", detailed); Assert.Contains("SOURCES", detailed); Assert.Contains("tick ", detailed);
        Assert.Equal(detailed, f.Commands.Execute("transcript " + f.Chat.ActiveConversation + " --details").Text);
        Assert.Contains("transcript --details", f.Commands.Execute("help").Text);
    }
    [Fact]
    public async Task RevocationDuringInferenceDiscardsEntireTurn()
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var f = new Fixture(new Inference(async (_,token) => { entered.SetResult(); await Task.Delay(Timeout.Infinite,token); return new(true,"late",""); }));
        var id = f.Chat.ActiveConversation!.Value; var pending = f.Talk("hello"); await entered.Task; f.Identity.Logout();
        Assert.Contains("cancelled",(await pending).Text); Assert.Empty(f.Engine.GetConversation(id).Messages);
    }
    [Fact]
    public async Task ChangedTranscriptRejectsStaleCompletionAndConsumedOperationCannotReplay()
    {
        var release = new TaskCompletionSource<InferenceReply>(TaskCreationOptions.RunContinuationsAsynchronously);
        var f = new Fixture(new Inference((_,_) => release.Task));
        var operation = f.Commands.Execute("talk hello").BrokerOperation!; var pending = operation(default);
        f.Engine.Talk(f.Chat.ActiveConversation!.Value,f.Engine.LocalOperator,"another turn");
        release.SetResult(new(true,"stale", "")); Assert.Contains("changed",(await pending).Text);
        Assert.Contains("consumed",(await operation(default)).Text); Assert.Equal(2,f.Engine.GetConversation(f.Chat.ActiveConversation.Value).Messages.Count);
    }
    [Fact]
    public async Task DisabledOrUnavailableModelUsesDeterministicFacts()
    {
        var f = new Fixture(new Inference((_,_)=>Task.FromResult(new InferenceReply(false,"","disabled"))));
        Assert.Contains("VERIFIED GRID STATE",(await f.Talk("status")).Text);
        Assert.Equal(2,f.Engine.GetConversation(f.Chat.ActiveConversation!.Value).Messages.Count);
    }
    [Fact]
    public async Task LongInputFallsBackWithoutTruncatingOrCallingModel()
    {
        var f = new Fixture(new Inference((_,_)=>throw new Exception("must not call")));
        var input = new string('界',1000); Assert.Contains("basic reply",(await f.Talk(input)).Text);
        Assert.Equal(input,f.Engine.GetConversation(f.Chat.ActiveConversation!.Value).Messages[0].Content);
    }
    [Fact]
    public async Task PromptIsBoundedAndIncludesOnlySelectedConversation()
    {
        string seen = ""; var f = new Fixture(new Inference((p,_)=>{seen=p;return Task.FromResult(new InferenceReply(true,"Suggestion: wait.",""));}));
        var secret = f.Engine.StartConversation(f.Engine.LocalOperator,f.Engine.EnsureCoordinator());
        f.Engine.Talk(secret,f.Engine.LocalOperator,"OTHER_TRANSCRIPT_SECRET");
        for(var i=0;i<20;i++) f.Engine.Talk(f.Chat.ActiveConversation!.Value,f.Engine.LocalOperator,"remember resource planning " + i);
        await f.Talk("resource planning");
        Assert.True(Encoding.UTF8.GetByteCount(seen)<=3200); Assert.Contains("EXTRACTIVE SUMMARY",seen);
        Assert.Contains("RECENT HISTORY",seen); Assert.DoesNotContain("OTHER_TRANSCRIPT_SECRET",seen);
        Assert.Contains("MCP: concise coordinator",seen);
    }
    [Theory]
    [InlineData("MCP", "coordinator")]
    [InlineData("MAXIS", "engineer")]
    [InlineData("SENTINEL", "security analyst")]
    [InlineData("JN", "archivist")]
    [InlineData("VOLT", "resource steward")]
    [InlineData("RELAY", "messenger")]
    public void NamedProfilesUseRegisteredRole(string name, string profile)
    {
        var f = new Fixture(Reply()); f.Engine.InitializeSystemAgents(f.Identity.Reference);
        var id = f.Engine.StartConversation(f.Engine.LocalOperator,f.Engine.ResolveProgram(name));
        Assert.Contains(profile,f.Engine.PrepareGroundedTurn(id,f.Engine.LocalOperator,"hello").Prompt);
    }
    [Fact]
    public void ProgramActorCannotRetrieveGlobalStateOrAnotherTranscript()
    {
        var f = new Fixture(Reply()); f.Engine.InitializeSystemAgents(f.Identity.Reference);
        var actor = f.Engine.ResolveProgram("MAXIS"); var id = f.Engine.StartConversation(actor,f.Engine.ResolveProgram("MCP"));
        var turn = f.Engine.PrepareGroundedTurn(id,actor,"status");
        Assert.DoesNotContain("districts ",turn.Facts); Assert.DoesNotContain("occupation",turn.Facts);
        Assert.Contains("global state and other agents' memories excluded",turn.Facts);
        Assert.Throws<ArgumentException>(()=>f.Engine.PrepareGroundedTurn(id,f.Engine.LocalOperator,"status"));
    }
    [Fact]
    public async Task ConfirmedVoiceUsesSameGroundingAndRetainsMicrophoneOrigin()
    {
        var f = new Fixture(Reply()); var result = f.Chat.SendVoice(f.Chat.ActiveConversation!.Value,"hello");
        await result.BrokerOperation!(default);
        Assert.Equal(MessageOrigin.Microphone,f.Engine.GetConversation(f.Chat.ActiveConversation.Value).Messages[0].Origin);
    }
    [Fact]
    public void RetrievedAgentMemoryIsScopedAndLabeledAsSimulation()
    {
        var world = new GridWorld(); var district = new GridDistrict { Name = "Community" }; world.AddDistrict(district);
        var first = new GridProgram { Name = "MCP", Kind = ResidentKind.Agent };
        var second = new GridProgram { Name = "MAXIS", Kind = ResidentKind.Agent };
        world.AddProgram(first, district.Id); world.AddProgram(second, district.Id);
        var engine = new GridSimulationEngine(world, TimeSpan.FromSeconds(10));
        engine.ConfigureSocial(first.Id, Grid.Simulation.Social.SocialPreference.Cooperative);
        engine.ConfigureSocial(second.Id, Grid.Simulation.Social.SocialPreference.Cooperative);
        engine.Step();
        Assert.NotEmpty(engine.GetSnapshot().Social.Residents.Single(r => r.ProgramId == first.Id).Memories);
        var id = engine.StartConversation(engine.LocalOperator, engine.ResolveProgram(first.Id.ToString()));
        var turn = engine.PrepareGroundedTurn(id, engine.LocalOperator, "What do you remember?");
        Assert.Contains("SIMULATED experience", turn.Prompt); Assert.Contains("social memory " + first.Id, turn.Sources);
        var privateId = engine.StartConversation(engine.ResolveProgram(second.Id.ToString()),engine.ResolveProgram(first.Id.ToString()));
        Assert.DoesNotContain("SIMULATED experience", engine.PrepareGroundedTurn(privateId,engine.ResolveProgram(second.Id.ToString()),"memory").Prompt);
    }
    [Fact]
    public async Task CancelledInferenceStoresNothing()
    {
        var f = new Fixture(Reply()); using var stop = new CancellationTokenSource(); stop.Cancel();
        await f.Commands.Execute("talk hello").BrokerOperation!(stop.Token);
        Assert.Empty(f.Engine.GetConversation(f.Chat.ActiveConversation!.Value).Messages);
    }
}
