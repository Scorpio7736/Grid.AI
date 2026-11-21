using Grid.Communication;
using Grid.Core;
using Grid.Persistence;
using Grid.Simulation;
using Microsoft.Data.Sqlite;
using Xunit;
namespace Grid.Terminal.Tests;

public sealed class MeetingTests
{
    private sealed class Model : ILocalInference
    {
        public string Prompt = "";
        public bool Available = true;
        public Task<InferenceReply> GenerateAsync(string prompt, CancellationToken token = default)
        { Prompt=prompt; return Task.FromResult(new InferenceReply(Available,"Shared answer only.","test")); }
    }
    private sealed class Fixture
    {
        public readonly TerminalSessionRegistry Registry = new();
        public readonly TerminalIdentitySession Owner = new(() => "meeting-owner");
        public readonly GridSimulationEngine Engine;
        public readonly Model Ai = new();
        public readonly CommandDispatcher Commands;
        public readonly ChatCommands Chat;
        public int Saves;
        public Fixture(GridSimulationSnapshot? snapshot = null)
        {
            Registry.Add(Owner);
            Engine = snapshot is null ? new(GridWorldFactory.CreateGenesis(),editAuthorizer:Registry) : GridSimulationEngine.Restore(snapshot,editAuthorizer:Registry);
            Owner.Attach(Engine); Owner.Login(); Engine.StartAgents(Owner.Reference);
            Commands=DefaultCommands.Create(Engine);
            Chat=ChatCommands.Register(Commands,Engine,Owner,Ai,_=> { Saves++; return Task.CompletedTask; }); Commands.UseIdentity(Owner);
        }
        public Guid Meeting(string names="MCP MAXIS JN")
        { Assert.Contains("Meeting:",Commands.Execute("agent meetings "+names+" -inline").Text); return Chat.ActiveConversation!.Value; }
        public Task<CommandResult> Talk(string input) => Commands.Execute("talk "+input).BrokerOperation!(default);
    }
    [Theory]
    [InlineData("hey maxis what is your favorite color", "MAXIS")]
    [InlineData("hello, jn read me something", "JN")]
    public async Task GreetingBeforeNameAddressesParticipant(string input, string name)
    {
        var f = new Fixture(); var id = f.Meeting(); await f.Talk(input);
        Assert.Equal(name, f.Engine.GetConversation(id).Messages.Last().Sender.Name);
    }
    [Fact]
    public async Task EachParticipantGetsIndependentResponseWithOneStoredUserMessage()
    {
        var f = new Fixture(); var id = f.Meeting();
        await f.Talk("each of you state a random fact");
        var messages = f.Engine.GetConversation(id).Messages;
        Assert.Equal(new[] { "Local operator", "MCP", "MAXIS", "JN" }, messages.Select(m => m.Sender.Name));
        Assert.Contains("respond only as JN", f.Ai.Prompt); Assert.Contains("never write another participant label", f.Ai.Prompt);
        Assert.Equal(2, f.Saves); Assert.Empty(f.Engine.GetSnapshot().Tasks.Records);
    }
    [Fact]
    public void NaturalAddressDoesNotOverrideValidManagementCommands()
    {
        var f = new Fixture(); f.Meeting();
        f.Commands.Register(new("maxis", "maxis workspaces", "test", _ => new("management")));
        Assert.Equal(new[] { "talk", "maxis what is your favorite color" }, f.Commands.ParseInput("maxis what is your favorite color"));
        Assert.Equal("management", f.Commands.Execute("maxis workspaces").Text);
        Assert.Equal("management", f.Commands.Execute("maxis register demo root project").Text);
    }
    [Fact]
    public void ChangedConversationRejectsWholeRoundWithoutPartialReplies()
    {
        var f = new Fixture(); var id = f.Meeting();
        const string input = "everyone state a fact";
        var turns = f.Engine.PrepareMeetingRound(id, f.Engine.LocalOperator, input, MessageOrigin.Terminal);
        f.Engine.EndConversation(id, f.Engine.LocalOperator);
        Assert.Throws<InvalidOperationException>(() => f.Engine.CompleteMeetingRound(turns, input, turns.Select(_ => new InferenceReply(true, "A fact", "test")).ToArray(), default));
        Assert.Empty(f.Engine.GetConversation(id).Messages);
    }
    [Theory]
    [InlineData("maxis")]
    [InlineData("maxis introduce yourself")]
    [InlineData("MAXIS, introduce yourself")]
    public async Task PlainNameSelectsActualParticipantAndSuppliesRoster(string input)
    {
        var f = new Fixture(); var id = f.Meeting(); await f.Talk(input);
        Assert.Equal("MAXIS", f.Engine.GetConversation(id).Messages.Last().Sender.Name);
        Assert.Contains("AUTHORITATIVE PARTICIPANTS", f.Ai.Prompt);
        Assert.Contains("MCP: Master Control Program", f.Ai.Prompt);
        Assert.Contains("JN: Grid.AI's file archivist", f.Ai.Prompt);
        Assert.Contains("respond only as MAXIS", f.Ai.Prompt);
    }
    [Fact]
    public void GroupIntroductionStoresOneUserMessageAndEveryParticipantInOrder()
    {
        var f = new Fixture(); var id = f.Meeting();
        var response = f.Commands.Execute("talk each of you introduce yourselves");
        Assert.Null(response.BrokerOperation);
        var messages = f.Engine.GetConversation(id).Messages;
        Assert.Equal(new[] { "Local operator", "MCP", "MAXIS", "JN" }, messages.Select(m => m.Sender.Name));
        Assert.Contains("software engineer", response.Text); Assert.Contains("file archivist", response.Text);
        Assert.Equal(new long[] { 1, 2, 3, 4 }, messages.Select(m => m.Sequence));
        Assert.Empty(f.Engine.GetSnapshot().Tasks.Records);
        Assert.Equal("", f.Ai.Prompt);
    }
    [Fact]
    public void NameOfAbsentAgentDoesNotSilentlyRouteToMcp()
    {
        var f = new Fixture(); f.Meeting("MCP JN");
        Assert.Contains("not in this meeting", f.Commands.Execute("talk MAXIS introduce yourself").Text);
        Assert.Empty(f.Engine.GetConversation(f.Chat.ActiveConversation!.Value).Messages);
    }
    [Theory]
    [InlineData("meetings MCP MAXIS JN -inline")]
    [InlineData("connect meetings mcp maxis -inline")]
    public void MeetingAliasesStartInlineWithoutLaunching(string command)
    {
        var f = new Fixture(); f.Commands.OpenConversation = _ => throw new Exception("Unexpected window");
        Assert.Contains("Meeting:", f.Commands.Execute(command).Text);
        Assert.True(f.Engine.GetConversation(f.Chat.ActiveConversation!.Value).IsMeeting);
    }
    [Theory]
    [InlineData("meetings MCP MAXIS")]
    [InlineData("connect meetings MCP MAXIS")]
    public void MeetingAliasesLaunchCanonicalValidatedRequest(string command)
    {
        var f = new Fixture(); IReadOnlyList<string>? launched = null;
        f.Commands.OpenConversation = tokens => { launched = tokens; return new("opened"); };
        Assert.Equal("opened", f.Commands.Execute(command).Text);
        Assert.Equal(new[] { "agent", "meetings", "MCP", "MAXIS" }, launched);
        launched = null;
        Assert.Contains("error", f.Commands.Execute(command.Replace("MAXIS", "Unknown")).Text, StringComparison.OrdinalIgnoreCase);
        Assert.Null(launched);
    }
    [Theory]
    [InlineData("MCP")]
    [InlineData("MCP mcp")]
    [InlineData("MCP Unknown")]
    [InlineData("MCP MAXIS -bogus")]
    [InlineData("MCP MAXIS JN TRON VOLT RELAY MCP")]
    public void InvalidParticipantsNeverLaunchOrMutate(string names)
    {
        var f=new Fixture(); var launches=0; f.Commands.OpenConversation=_=> {launches++;return new("opened");};
        var result=f.Commands.Execute("agent meetings "+names);
        Assert.DoesNotContain("Opened",result.Text); Assert.Empty(f.Engine.GetSnapshot().Communications.Conversations); Assert.Equal(0,launches);
    }
    [Theory]
    [InlineData("How do I build and test this code?","MAXIS")]
    [InlineData("Review the firewall and malware protection","TRON")]
    [InlineData("Find duplicate files and storage quota","JN")]
    [InlineData("CPU GPU RAM performance","VOLT")]
    [InlineData("Notification delivery inbox","RELAY")]
    [InlineData("Coordinate and delegate tasks","MCP")]
    [InlineData("Hello everyone","MCP")]
    [InlineData("build storage","JN")]
    [InlineData("@MAXIS: storage question","MAXIS")]
    public async Task ExactlyOneSelectedParticipantReplies(string input,string expected)
    {
        var f=new Fixture(); var id=f.Meeting("RELAY VOLT TRON JN MAXIS MCP");
        var result=await f.Talk(input); var messages=f.Engine.GetConversation(id).Messages;
        Assert.Equal(2,messages.Count); Assert.Equal(expected,messages[1].Sender.Name); Assert.NotNull(result.AnimateFrom);
        Assert.Contains(result.Presentation!,s=>s.Tone==ConversationTone.Heading && result.Text.Substring(s.Start,s.Length)==expected+": ");
        var body=Assert.Single(result.Presentation!,s=>s.Tone==ConversationTone.Agent);
        Assert.Equal("Shared answer only.",result.Text.Substring(body.Start,body.Length));
        Assert.Contains(expected+":",result.Text); Assert.Empty(f.Engine.GetSnapshot().Tasks.Records);
        Assert.Contains("SHARED MEETING",f.Ai.Prompt); Assert.Equal(2,f.Saves);
    }
    [Fact]
    public async Task NoMatchWithoutMcpUsesAlphabeticalRoleAndFallbackIsHonest()
    {
        var f=new Fixture(); f.Ai.Available=false; var id=f.Meeting("MAXIS JN");
        var result=await f.Talk("hello");
        Assert.Equal("JN",f.Engine.GetConversation(id).Messages[1].Sender.Name);
        Assert.Contains("No specialty match",result.Text); Assert.Contains("cannot provide an AI answer",result.Text); Assert.NotNull(result.AnimateFrom);
        Assert.Empty(f.Engine.GetSnapshot().Tasks.Records);
    }
    [Theory]
    [InlineData("@TRON review security")]
    [InlineData("@MAXIS")]
    [InlineData("@MAXIS: ")]
    public void BadDirectionDoesNotSilentlyRerouteOrAppend(string input)
    {
        var f=new Fixture(); var id=f.Meeting(); var result=f.Commands.Execute("talk "+input);
        Assert.Null(result.BrokerOperation); Assert.Contains("Error",result.Text); Assert.Empty(f.Engine.GetConversation(id).Messages);
    }
    [Fact]
    public async Task MeetingIncludesSharedHistoryButNotPrivateConversationOrSocialMemory()
    {
        var f=new Fixture();
        var privateId=f.Engine.StartConversation(f.Engine.LocalOperator,f.Engine.ResolveProgram("MAXIS"));
        f.Engine.Talk(privateId,f.Engine.LocalOperator,"PRIVATE-CONVERSATION-SECRET");
        var a=f.Engine.ResolveProgram("MAXIS"); var b=f.Engine.ResolveProgram("JN");
        f.Engine.ConfigureSocial(a.Id,Grid.Simulation.Social.SocialPreference.Cooperative);
        f.Engine.ConfigureSocial(b.Id,Grid.Simulation.Social.SocialPreference.Cooperative);
        f.Engine.Pause(); f.Engine.Step(); f.Engine.StartAgents(f.Owner.Reference);
        var id=f.Meeting(); await f.Talk("@MAXIS build plan"); await f.Talk("@JN file plan");
        Assert.DoesNotContain("PRIVATE-CONVERSATION-SECRET",f.Ai.Prompt);
        Assert.DoesNotContain("social memory",f.Ai.Prompt); Assert.DoesNotContain("SIMULATED experience",f.Ai.Prompt);
        Assert.Contains("Shared answer only.",f.Ai.Prompt); Assert.Contains("file plan",f.Ai.Prompt);
        Assert.Equal(new long[]{1,2,3,4},f.Engine.GetConversation(id).Messages.Select(m=>m.Sequence));
    }
    [Fact]
    public void ConcurrentTurnsPauseAndEndedMeetingsCannotCommitStaleReplies()
    {
        var f=new Fixture(); var id=f.Meeting();
        var first=f.Engine.PrepareGroundedTurn(id,f.Engine.LocalOperator,"build");
        var stale=f.Engine.PrepareGroundedTurn(id,f.Engine.LocalOperator,"storage");
        f.Engine.CompleteGroundedTurn(first,new(true,"first","test"));
        Assert.Throws<InvalidOperationException>(()=>f.Engine.CompleteGroundedTurn(stale,new(true,"stale","test")));
        var paused=f.Engine.PrepareGroundedTurn(id,f.Engine.LocalOperator,"storage"); f.Engine.Pause(); f.Engine.StartAgents(f.Owner.Reference);
        Assert.Throws<InvalidOperationException>(()=>f.Engine.CompleteGroundedTurn(paused,new(true,"stale","test")));
        f.Engine.EndConversation(id,f.Engine.LocalOperator);
        Assert.Throws<InvalidOperationException>(()=>f.Engine.Talk(id,f.Engine.LocalOperator,"hello"));
        Assert.Equal(2,f.Engine.GetConversation(id).Messages.Count);
    }
    [Fact]
    public void AgentRoleCannotCreateMeetingOrBorrowHumanGrounding()
    {
        var f=new Fixture(); var id=f.Meeting(); f.Owner.Switch(f.Engine.ResolveProgram("MAXIS").Id.ToString());
        Assert.Throws<UnauthorizedAccessException>(()=>f.Engine.StartMeeting(f.Owner.Reference,["MCP","JN"]));
        Assert.Throws<UnauthorizedAccessException>(()=>f.Engine.PrepareGroundedTurn(id,f.Engine.ResolveProgram("MAXIS"),"private context"));
        f.Owner.Switch(f.Engine.ResolveProgram("TRON").Id.ToString());
        Assert.DoesNotContain(id.ToString(),f.Commands.Execute("conversations").Text);
        Assert.DoesNotContain("Shared answer",f.Commands.Execute("transcript "+id).Text);
        Assert.Empty(f.Engine.GetSnapshot().Tasks.Records);
    }
    [Fact]
    public async Task ReviewedVoiceUsesSameSelectedSpeakerAndOrigin()
    {
        var f=new Fixture(); var id=f.Meeting(); var reply=f.Chat.SendVoice(id,"@JN review files"); await reply.BrokerOperation!(default);
        var messages=f.Engine.GetConversation(id).Messages;
        Assert.Equal(MessageOrigin.Microphone,messages[0].Origin); Assert.Equal("JN",messages[1].Sender.Name);
    }
    [Fact]
    public async Task SQLiteUpgradePreservesDirectConversationAndMeetingCanResume()
    {
        var f=new Fixture(); var direct=f.Engine.StartConversation(f.Engine.LocalOperator,f.Engine.ResolveProgram("MCP")); f.Engine.Talk(direct,f.Engine.LocalOperator,"old direct message");
        var folder=Path.Combine(Path.GetTempPath(),"GridMeeting-"+Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
        try
        {
            var path=Path.Combine(folder,"world.db"); var state=f.Engine.GetSnapshot(); var info=new WorldInfo(state.WorldId,"Meeting",DateTimeOffset.UtcNow);
            using(var initial=new WorldStore(path)) initial.Create(info,state);
            using(var db=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=path,Pooling=false}.ToString()))
            {db.Open();using var q=db.CreateCommand();q.CommandText="DELETE FROM SchemaMigrations WHERE Version>=22; PRAGMA user_version=21;";q.ExecuteNonQuery();}
            using var store=new WorldStore(path); var next=new Fixture(store.Load().State); var meeting=next.Meeting(); await next.Talk("@MAXIS build question"); store.Save(info,next.Engine.GetSnapshot());
            var restored=new Fixture(store.Load().State);
            Assert.False(restored.Engine.GetConversation(direct).IsMeeting); Assert.True(restored.Engine.GetConversation(meeting).IsMeeting);
            Assert.Equal(4,restored.Engine.GetConversation(meeting).Participants.Count);
            Assert.Contains("Resumed",restored.Commands.Execute("resume "+meeting+" -inline").Text);
            await restored.Talk("@JN files"); Assert.Equal(4,restored.Engine.GetConversation(meeting).Messages.Count);
            Assert.Contains("MCP, MAXIS, JN",restored.Commands.Execute("transcript").Text);
            Assert.Contains("old direct message",restored.Engine.GetConversation(direct).Messages[0].Content);
        }
        finally {Directory.Delete(folder,true);}
    }
    [Fact]
    public void InvalidMeetingShapeCannotBeRestored()
    {
        var f=new Fixture(); var id=f.Meeting(); var state=f.Engine.GetSnapshot(); var c=f.Engine.GetConversation(id);
        foreach(var broken in new[]{c with{Participants=[c.Participants[0],null!,c.Participants[2]]},c with{IsMeeting=false},c with{Participants=[c.Participants[0],c.Participants[1],c.Participants[1]]},c with{Participants=c.Participants.Skip(1).ToArray()}})
            Assert.Throws<InvalidDataException>(()=>GridSimulationEngine.Restore(state with{Communications=state.Communications with{Conversations=[broken]}}));
    }
}
