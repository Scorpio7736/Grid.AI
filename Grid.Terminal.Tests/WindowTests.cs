using System.IO.Pipes;
using Grid.Core;
using Grid.Communication;
using Grid.Simulation;
using Xunit;
namespace Grid.Terminal.Tests;

public sealed class WindowTests
{
    [Fact]
    public async Task AttachedMeetingRunsReviewedDemonstrationWithoutIdsAndCancelsAnotherPlan()
    {
        await using var f = new Fixture(mcp: true); f.Engine.StartAgents(f.Owner.Reference);
        await using var child = await f.Open("agent meetings MCP MAXIS JN");
        Assert.Contains("DRAFT ONLY", (await child.Send("command", "plan to have MAXIS demonstrate the message meeting-check")).Text);
        await child.Send("command", "submit plan");
        await child.Send("command", "run this plan");
        Assert.Equal(0, Assert.Single(f.Engine.GetSnapshot().Tasks.Records).Attempts);
        await child.Send("command", "approve this plan");
        Assert.Contains("Completed", (await child.Send("command", "run this plan")).Text);
        await child.Send("command", "run this plan");
        Assert.Equal(1, Assert.Single(f.Engine.GetSnapshot().Tasks.Records).Attempts);
        await child.Send("command", "plan to have MAXIS demonstrate the message cancelled-check");
        await child.Send("command", "submit plan"); await child.Send("command", "cancel this plan");
        Assert.Contains(f.Engine.GetSnapshot().Tasks.Records, t => t.Status == Grid.Simulation.Tasks.TaskStatus.Cancelled && t.Attempts == 0);
    }
    private sealed class InvitationInference : ILocalInference
    {
        public Task<InferenceReply> GenerateAsync(string prompt, CancellationToken token = default) => Task.FromResult(new InferenceReply(true,
            "{\"steps\":[{\"agent\":\"VOLT\",\"capability\":\"volt.status\",\"argument\":\"current\"}]}", "test"));
    }
    [Fact]
    public async Task SpecialistCanJoinPlanAndReportInSameAttachedWindow()
    {
        await using var f = new Fixture(inference: new InvitationInference(), mcp: true); f.Engine.StartAgents(f.Owner.Reference);
        await using var child = await f.Open("connect MCP");
        Assert.Contains("suggests inviting VOLT", (await child.Send("command", "invite VOLT")).Text);
        Assert.Contains("Invited VOLT", (await child.Send("command", "accept invitation")).Text);
        Assert.Contains("DRAFT ONLY", (await child.Send("command", "@VOLT plan current resource status")).Text);
        Assert.Contains("await approval", (await child.Send("command", "submit plan")).Text);
        var plan = Assert.Single(f.Engine.GetSnapshot().Tasks.Plans); var task = Assert.Single(plan.TaskIds);
        Assert.Contains("Approved", (await child.Send("command", "approve this plan")).Text);
        Assert.Contains("Completed", (await child.Send("command", "run this plan")).Text);
        Assert.Contains("Completed", (await child.Send("command", "MCP progress")).Text);
        await child.Send("command", "remove participant VOLT");
        Assert.Contains("Removed VOLT", (await child.Send("command", "confirm removal")).Text);
        Assert.Equal("GRID\n\nCurrent participants: MCP", (await child.Send("command", "participants")).Text);
    }
    [Fact, Trait("Phase", "6.8")]
    public async Task AttachedDirectChatAndPickerRunAnalysisWithoutContinue()
    {
        await using var f = new Fixture(inference: new WindowInference());
        f.Engine.StartAgents(f.Owner.Reference);
        await using var client = await f.Open("connect MCP");
        var reply = await client.Send("command", "What's a div with id \"Test\"?");
        Assert.Contains("Suggestion: use status", reply.Text); Assert.NotNull(reply.AnimateFrom);
        var picker = await client.Send("command", "select files"); Assert.True(picker.PickFiles);
        var path = Path.Combine(Path.GetTempPath(), "Grid68-" + Guid.NewGuid() + ".html");
        try
        {
            File.WriteAllText(path, "<h1>INDIGO</h1>");
            reply = await client.Send("context-files", files: [path]);
            Assert.Contains("Suggestion: use status", reply.Text); Assert.NotNull(reply.AnimateFrom);
            Assert.NotNull(reply.Presentation); Assert.Empty(f.Engine.GetSnapshot().Tasks.Records);
        }
        finally { File.Delete(path); }
    }
    [Fact, Trait("Phase", "6.9")]
    public async Task AttachedFileOperationsRunOnHostAndParentRevocationStopsApproval()
    {
        var folder = Path.Combine(Path.GetTempPath(), "Grid69Window-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(folder, "source"));
        File.WriteAllText(Path.Combine(folder, "source", "sample.csproj"), "<Project />");
        try
        {
            await using var f = new Fixture(); f.Engine.StartAgents(f.Owner.Reference);
            f.Engine.ConfigureMaxisRuntime(Path.Combine(folder, "artifacts"));
            f.Engine.RegisterDevelopmentWorkspace(f.Owner.Reference, "demo", Path.Combine(folder, "source"), "sample.csproj");
            await using var client = await f.Open("connect MCP");
            var path = Path.Combine(folder, "source", "note.md");
            Assert.Contains("CREATE", (await client.Send("command", "create file note.md in demo with content INDIGO")).Text);
            await client.Send("command", "approve this change"); Assert.False(File.Exists(path));
            await client.Send("command", "apply this change"); Assert.Equal("INDIGO", File.ReadAllText(path));
            Assert.Contains("INDIGO", (await client.Send("command", "read file note.md in demo")).Text);
            await client.Send("command", "update file note.md in demo with content UPDATED");
            await client.Send("command", "approve this change"); await client.Send("command", "apply this change");
            Assert.Equal("UPDATED", File.ReadAllText(path));
            await client.Send("command", "delete file note.md in demo");
            await client.Send("command", "approve high-risk"); await client.Send("command", "apply this change");
            Assert.False(File.Exists(path)); await client.Send("command", "undo this change"); Assert.Equal("UPDATED", File.ReadAllText(path));
            await client.Send("command", "delete file note.md in demo"); await client.Send("command", "approve high-risk");
            f.Owner.Logout(); Assert.True((await client.Send("command", "apply this change")).Closed);
            Assert.Equal("UPDATED", File.ReadAllText(path));
        }
        finally { Directory.Delete(folder, true); }
    }
    private sealed class FileDraftInference : ILocalInference
    {
        public Task<InferenceReply> GenerateAsync(string prompt, CancellationToken token = default) => Task.FromResult(new InferenceReply(true, "{\"content\":\"WINDOW DRAFT\"}", "controlled"));
    }
    [Fact, Trait("Phase", "6.9")]
    public async Task AttachedAiDraftStaysUnapprovedAndCannotBeAppliedFromAnotherWindow()
    {
        var folder = Path.Combine(Path.GetTempPath(), "Grid69DraftWindow-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(folder, "source")); File.WriteAllText(Path.Combine(folder, "source", "sample.csproj"), "<Project />");
        try
        {
            await using var f = new Fixture(inference: new FileDraftInference()); f.Engine.StartAgents(f.Owner.Reference);
            f.Engine.ConfigureMaxisRuntime(Path.Combine(folder, "artifacts"));
            f.Engine.RegisterDevelopmentWorkspace(f.Owner.Reference, "demo", Path.Combine(folder, "source"), "sample.csproj");
            await using var client = await f.Open("connect MCP"); await using var other = await f.Open("connect MCP");
            var response = await client.Send("command", "create file ai.md in demo with instructions Write a note.");
            Assert.Contains("WINDOW DRAFT", response.Text); var path = Path.Combine(folder, "source", "ai.md"); Assert.False(File.Exists(path));
            var id = System.Text.RegularExpressions.Regex.Match(response.Text, @"Change ([a-f0-9-]{36})").Groups[1].Value;
            Assert.NotEmpty(id); await client.Send("command", "approve this change");
            Assert.DoesNotContain("Applied demo", (await other.Send("command", "changes apply " + id)).Text); Assert.False(File.Exists(path));
            await client.Send("command", "apply this change"); Assert.Equal("WINDOW DRAFT", File.ReadAllText(path));
        }
        finally { Directory.Delete(folder, true); }
    }
    private sealed class ChatProbeBackend : IInferenceBackend
    {
        public int Calls;
        public Task<string> GenerateAsync(string prompt, CancellationToken token)
        { Calls++; return Task.FromResult("Hello from the shared local model."); }
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InlineAndAttachedChatsShareRuntimeAndPreserveStartupReason(bool ready)
    {
        var backend = new ChatProbeBackend();
        const string reason = "Local AI assets missing. Missing: assets.json; Runtime root: PC2/runtime";
        await using var inference = new SharedLocalInference(backend, enabled: ready, unavailableReason: reason);
        await using var f = new Fixture(inference: inference);
        f.Engine.StartAgents(f.Owner.Reference);
        f.Commands.Execute("connect MCP -inline");
        var inline = await f.Commands.Execute("Hello MCP").BrokerOperation!(default);
        f.Commands.Execute("return to terminal");
        await using var child = await f.Open("connect MCP");
        var attached = await child.Send("command", "Hello MCP");
        foreach (var text in new[] { inline.Text, attached.Text })
        {
            if (ready)
            {
                Assert.Contains("Hello from the shared local model.", text);
                Assert.DoesNotContain("using a basic reply", text);
            }
            else
            {
                Assert.Contains(reason, text);
                Assert.Contains("ai status", text);
                Assert.Contains("using a basic reply", text);
            }
        }
        Assert.Equal(ready ? 2 : 0, backend.Calls);
    }
    private sealed class Fixture : IAsyncDisposable
    {
        public readonly TerminalSessionRegistry Registry = new();
        public readonly TerminalIdentitySession Owner;
        public readonly GridSimulationEngine Engine;
        public readonly WindowHub Hub;
        public readonly CommandDispatcher Commands;
        public string Endpoint = "";
        public Fixture(Func<string, int>? launch = null, ILocalInference? inference = null, IScreenContextProvider? screens = null, bool mcp = false)
        {
            Owner = new(() => "test-owner"); Registry.Add(Owner);
            Engine = new(GridWorldFactory.CreateGenesis(), editAuthorizer: Registry); Owner.Attach(Engine); Owner.Login(); Engine.PrepareChatFixture();
            CommandDispatcher Create(TerminalIdentitySession identity)
            {
                var commands = DefaultCommands.Create(Engine); ChatCommands.Register(commands, Engine, identity, inference, save: mcp ? _ => Task.CompletedTask : null, screenProvider: screens);
                if (mcp) TaskCommands.Register(commands, Engine, identity, _ => Task.CompletedTask);
                IdentityCommands.Register(commands, Engine, identity, interactiveUi: true);
                ContextCommands.Register(commands, Engine, identity);
                WorkspaceChangeCommands.Register(commands, Engine, identity, inference);
                commands.UseIdentity(identity); return commands;
            }
            Hub = new(Engine, Registry, Create, name => { Endpoint = name; return launch?.Invoke(name) ?? Environment.ProcessId; });
            Commands = Create(Owner); Hub.Register(Commands, Owner);
        }
        public ValueTask DisposeAsync() => Hub.DisposeAsync();
        public async Task<Client> Open(string command = "ntm")
        {
            Assert.Contains("Opened", Commands.Execute(command).Text);
            var client = new Client(Endpoint); await client.Connect(); return client;
        }
    }
    private sealed class Client(string endpoint) : IAsyncDisposable
    {
        private readonly NamedPipeClientStream pipe = new(".", endpoint, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
        public WindowReply Greeting = null!;
        public async Task Connect()
        {
            await pipe.ConnectAsync(5000); using var timeout = new CancellationTokenSource(5000);
            Greeting = await WindowProtocol.ReadAsync<WindowReply>(pipe, timeout.Token);
        }
        public async Task<WindowReply> Send(string kind, string? line = null, string[]? files = null)
        {
            using var timeout = new CancellationTokenSource(5000);
            await WindowProtocol.WriteAsync(pipe, new WindowRequest(kind, line, Files: files), timeout.Token);
            return await WindowProtocol.ReadAsync<WindowReply>(pipe, timeout.Token);
        }
        public ValueTask DisposeAsync() => pipe.DisposeAsync();
    }
    private sealed class WindowInference : ILocalInference
    {
        public async Task<InferenceReply> GenerateAsync(string prompt, CancellationToken token = default)
        { await Task.Delay(50, token); return new(true, "Suggestion: use status.", "test"); }
    }
    [Fact]
    public async Task ScreenAssistanceUsesWindowScopeAndPropagatesStyledReply()
    {
        var provider=new ScreenTests.Provider();
        await using var f=new Fixture(inference:new WindowInference(),screens:provider);
        f.Engine.StartAgents(f.Owner.Reference);
        await using var client=await f.Open("connect MCP");
        Assert.Contains("LEFT",(await client.Send("command","look at my screen")).Text);
        Assert.Contains("selected",(await client.Send("command","1")).Text);
        var reply=await client.Send("command","look at my screen and explain the text");
        Assert.Contains("Suggestion: use status",reply.Text); Assert.NotNull(reply.AnimateFrom);
        Assert.Contains(reply.Presentation!,s=>s.Tone==ConversationTone.Agent);
        Assert.Equal(1,provider.Captures);
        Assert.Empty(f.Engine.GetSnapshot().Communications.Conversations.Single().Messages);
        Assert.Contains("Select a screen first",f.Commands.Execute("screen ask Explain").Text);
        Assert.Contains("stopped",(await client.Send("command","stop screen")).Text, StringComparison.OrdinalIgnoreCase);
        f.Owner.Logout(); Assert.True((await client.Send("ping")).Closed);
    }

    [Fact]
    public async Task ConversationWindowAwaitsAssistanceWithoutExecutingTasks()
    {
        await using var f=new Fixture(inference:new WindowInference()); f.Engine.StartAgents(f.Owner.Reference);
        await using var client=await f.Open("connect MCP");
        var reply=await client.Send("command","assist advice How should I inspect status?");
        Assert.Contains("Suggestion: use status.",reply.Text);Assert.NotNull(reply.AnimateFrom);
        var style=Assert.Single(reply.Presentation!,s=>s.Tone==ConversationTone.Agent);
        Assert.Equal("Suggestion: use status.",reply.Text.Substring(style.Start,style.Length)); Assert.Empty(f.Engine.GetSnapshot().Tasks.Records);
    }
    [Fact]
    public async Task PrivateConversationWindowSupportsMemoryAndLogoutRevokesIt()
    {
        await using var f = new Fixture(); f.Engine.StartAgents(f.Owner.Reference);
        await using var client = await f.Open("connect MCP");
        Assert.Contains("Memory project:",(await client.Send("command","project grid")).Text);
        Assert.Contains("Saved",(await client.Send("command","remember fact release: Release codename is Moonstone.")).Text);
        Assert.Contains("Moonstone",(await client.Send("command","show memories")).Text);
        Assert.Contains("Corrected",(await client.Send("command","correct memory release: Release codename is Sunstone.")).Text);
        Assert.Contains("Forgotten",(await client.Send("command","forget memory release")).Text);
        f.Owner.Logout(); Assert.True((await client.Send("ping")).Closed);
    }
    [Fact]
    public async Task ConversationWindowAwaitsSharedInferenceAndPreservesAnimation()
    {
        await using var f = new Fixture(inference: new WindowInference());
        await using var client = await f.Open("connect MCP");
        var reply = await client.Send("command", "talk hello");
        Assert.Contains("MCP: Suggestion:", reply.Text); Assert.DoesNotContain("SOURCES", reply.Text);
        Assert.Contains(reply.Presentation!,s=>s.Tone==ConversationTone.User);
        Assert.Contains(reply.Presentation!,s=>s.Tone==ConversationTone.Agent);
        var transcript=await client.Send("command","show our conversation");
        Assert.Null(transcript.AnimateFrom);Assert.Contains(transcript.Presentation!,s=>s.Tone==ConversationTone.Agent);
        Assert.False((await client.Send("command","turn off animation")).Typewriter);
        Assert.True((await client.Send("command","turn on animation")).Typewriter);
        Assert.Equal("You> ", reply.Prompt); Assert.DoesNotContain("capabilities:",client.Greeting.Text);
        Assert.Contains("SOURCES", (await client.Send("command", "show conversation details")).Text); Assert.NotNull(reply.AnimateFrom);
        Assert.Equal(2, f.Engine.GetSnapshot().Communications.Conversations.Single().Messages.Count);
        Assert.False((await client.Send("ping")).Closed);
    }
    [Fact]
    public async Task AttachedPickerUsesScopedPendingRequestAndSupportsHeartbeat()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".md");
        File.WriteAllText(path, "ATTACHED PICKED CONTENT");
        try
        {
            await using var f = new Fixture(); await using var client = await f.Open();
            Assert.Contains("no file selection", (await client.Send("context-files", files: [path])).Text);
            Assert.True((await client.Send("command", "context add -search")).PickFiles);
            Assert.False((await client.Send("ping")).Closed);
            Assert.Contains("Imported", (await client.Send("context-files", files: [path])).Text);
            Assert.Contains("ATTACHED PICKED CONTENT", (await client.Send("command", "context read " + Path.GetFileName(path))).Text);
            Assert.Contains("no file selection", (await client.Send("context-files", files: [path])).Text);
            Assert.DoesNotContain("ATTACHED PICKED CONTENT", f.Commands.Execute("context read " + Path.GetFileName(path)).Text);
        }
        finally { File.Delete(path); }
    }
    [Fact]
    public async Task TerminalsShareOneEngineAndExitOnlyDetaches()
    {
        await using var f = new Fixture(); await using var first = await f.Open(); await using var second = await f.Open();
        Assert.Equal("terminal", first.Greeting.Mode);
        f.Engine.Pause(); await first.Send("command", "step");
        Assert.Equal(1, f.Engine.TickCount);
        Assert.Contains("Tick: 1", (await second.Send("command", "status")).Text);
        Assert.True((await first.Send("command", "/close")).Closed);
        await second.Send("command", "step"); Assert.Equal(2, f.Engine.TickCount);
        Assert.Contains("Tick: 2", f.Commands.Execute("status").Text);
    }
    [Fact]
    public async Task AttachedMasterCanPauseAndResumeButCannotTransfer()
    {
        await using var f = new Fixture(); await using var child = await f.Open();
        Assert.Contains("paused",(await child.Send("command","grid -pause")).Text);
        Assert.False(f.Engine.IsRunning);
        Assert.Contains("running",(await child.Send("command","grid -start")).Text);
        Assert.True(f.Engine.IsRunning);
        Assert.Contains("original host",(await child.Send("command","grid -copyto -select")).Text);
    }
    [Fact]
    public async Task ChatAndEditPreviewsAreIndependentButAuthorizedEditsReachSharedEngine()
    {
        await using var f = new Fixture(); await using var first = await f.Open(); await using var second = await f.Open();
        Assert.Contains("Opened conversation", (await first.Send("command", "connect")).Text);
        await using var conversation = new Client(f.Endpoint); await conversation.Connect();
        Assert.Contains("MCP", (await second.Send("command", "talk hello")).Text);
        Assert.Contains("MCP", f.Commands.Execute("talk hello").Text);
        Assert.Equal(3, f.Engine.GetSnapshot().Communications.Conversations.Count);
        var preview = await first.Send("command", "edit MCP occupation WindowTest");
        var id = System.Text.RegularExpressions.Regex.Match(preview.Text, @"Preview ([0-9a-f-]+)").Groups[1].Value;
        Assert.NotEmpty(id);
        await second.Send("command", "return to terminal");
        Assert.Contains("No matching preview", (await second.Send("command", "apply-edit " + id)).Text);
        Assert.Contains("Applied", (await first.Send("command", "apply-edit " + id)).Text);
        Assert.Equal("WindowTest", f.Engine.GetSnapshot().Programs.Single(p => p.Name == "MCP").Occupation);
    }
    [Fact]
    public async Task AgentAttachmentCannotPromoteOrMutateAndParentLogoutRevokesIt()
    {
        await using var f = new Fixture(); var agent = f.Engine.CreateRunningCoordinator(); f.Engine.RegisterAgentIdentity(agent.Id); f.Owner.Switch(agent.Id.ToString());
        await using var child = await f.Open();
        Assert.Contains("scoped", (await child.Send("command", "switch master")).Text);
        Assert.Contains("denied", (await child.Send("command", "step")).Text);
        Assert.DoesNotContain(child.Greeting.Commands!, c => c.Name == "step");
        f.Owner.Logout(); Assert.True((await child.Send("ping")).Closed);
        Assert.Equal(0, f.Engine.TickCount);
    }
    [Fact]
    public async Task SnapshotIsFixedReadOnlyAndMutatingFlagsAreRejected()
    {
        await using var f = new Fixture();
        Assert.Contains("read-only", f.Commands.Execute("step -opninwin").Text); Assert.Equal(0, f.Engine.TickCount);
        await using var viewer = await f.Open("status -opninwin");
        Assert.Equal("snapshot", viewer.Greeting.Mode); Assert.Contains("Tick: 0", viewer.Greeting.Text);
        f.Engine.Pause(); f.Commands.Execute("step");
        Assert.Contains("read-only", (await viewer.Send("command", "step")).Text);
        Assert.Equal(1, f.Engine.TickCount); Assert.Equal("", (await viewer.Send("ping")).Text);
    }
    [Fact]
    public async Task LiveFramesAreSharedAndRevokedOnRoleChange()
    {
        await using var f = new Fixture(); await using var viewer = await f.Open("live -opninwin");
        var frame = await viewer.Send("frame"); Assert.True(frame.Live); Assert.NotEmpty(frame.Text);
        var agent = f.Engine.CreateRunningCoordinator(); f.Engine.RegisterAgentIdentity(agent.Id); f.Owner.Switch(agent.Id.ToString());
        Assert.True((await viewer.Send("frame")).Closed);
    }
    [Fact]
    public async Task PendingAttachmentCannotReadSnapshotAfterRevocation()
    {
        await using var f = new Fixture(); Assert.Contains("Opened", f.Commands.Execute("status -opninwin").Text);
        f.Owner.Logout(); await using var client = new Client(f.Endpoint); await client.Connect();
        Assert.True(client.Greeting.Closed); Assert.DoesNotContain("Tick:", client.Greeting.Text);
    }
    [Fact]
    public async Task StopDisconnectsClientsAndRefusesNewWindows()
    {
        await using var f = new Fixture(); await using var client = await f.Open();
        await f.Hub.StopAsync();
        await Assert.ThrowsAnyAsync<IOException>(() => client.Send("ping"));
        Assert.Contains("shutting down", f.Commands.Execute("ntm").Text);
    }
    [Fact]
    public async Task LaunchFailureLeavesHostUsableAndHelpIncludesWindowSyntax()
    {
        await using var f = new Fixture(_ => throw new IOException("test launch failure"));
        Assert.Contains("could not open", f.Commands.Execute("ntm").Text);
        Assert.Contains("Tick: 0", f.Commands.Execute("status").Text);
        Assert.Contains("-opninwin", f.Commands.Execute("help").Text);
        Assert.Contains("status -opninwin", CommandHelp.Complete("status -o", f.Commands.Commands));
        Assert.Contains("ntm", CommandHelp.Complete("nt", f.Commands.Commands));
    }
    [Fact]
    public async Task AttachedLifecycleCommandsStayOnOwnerAndLogoutDoesNotRevokeParent()
    {
        await using var f = new Fixture(); await using var client = await f.Open();
        foreach (var command in new[] { "nge", "grid -copyto something", "grid -transferto something", "tranexist something", "copexist something", "credentials", "login" })
            Assert.Contains("original host", (await client.Send("command", command)).Text);
        Assert.True((await client.Send("command", "logout")).ConfirmLogout);
        Assert.False((await client.Send("ping")).Closed);
        Assert.True((await client.Send("logout")).Closed);
        Assert.True(f.Owner.IsMaster);
    }
    [Fact]
    public async Task InlineEscapePreservesOriginalVoiceConversationAndDoesNotLaunch()
    {
        await using var f = new Fixture(_ => throw new InvalidOperationException("Should not launch"));
        Assert.Contains("Connected", f.Commands.Execute("connect MCP -inline").Text);
        Assert.Contains("VERIFIED", f.Commands.Execute("talk status").Text);
        Assert.Equal(2, Assert.Single(f.Engine.GetSnapshot().Communications.Conversations).Messages.Count);
        Assert.Contains("only once", f.Commands.Execute("connect -inline -inline").Text);
        Assert.Contains("connect -inline", CommandHelp.Complete("connect -i", f.Commands.Commands));
    }

    [Fact]
    public async Task MeetingWindowUsesTypewriterSharedHistoryAndResumeAfterDetach()
    {
        await using var f = new Fixture(); f.Engine.StartAgents(f.Owner.Reference);
        await using var child = await f.Open("agent meetings MCP MAXIS JN");
        Assert.Equal("conversation",child.Greeting.Mode); Assert.Contains("Meeting:",child.Greeting.Text);
        var reply=await child.Send("command","talk @MAXIS build this"); Assert.NotNull(reply.AnimateFrom); Assert.Contains("MAXIS:",reply.Text);
        var meeting=Assert.Single(f.Engine.GetSnapshot().Communications.Conversations); Assert.True(meeting.IsMeeting);
        Assert.True((await child.Send("command","/close")).Closed);
        Assert.Equal(ConversationStatus.Open,f.Engine.GetConversation(meeting.Id).Status);
        await using var resumed=await f.Open("resume "+meeting.Id);
        Assert.Contains("JN:",(await resumed.Send("command","talk @JN files")).Text);
        Assert.Equal(4,f.Engine.GetConversation(meeting.Id).Messages.Count);
        f.Owner.Logout(); Assert.True((await resumed.Send("ping")).Closed);
    }
    [Fact]
    public async Task DedicatedConversationStoresCompleteTurnsAndClosePreservesResume()
    {
        await using var f = new Fixture();
        await using var child = await f.Open("connect MCP");
        Assert.Equal("conversation", child.Greeting.Mode);
        Assert.Contains("Connected to MCP", child.Greeting.Text);
        Assert.DoesNotContain(child.Greeting.Commands!, c => c.Name is "step" or "voice" or "switch");
        var connectedId = Assert.Single(f.Engine.GetSnapshot().Communications.Conversations).Id;
        Assert.Contains("MCP", f.Commands.Execute("talk hello").Text);
        var response = await child.Send("command", "talk status");
        Assert.NotNull(response.AnimateFrom);
        Assert.Contains("MCP", response.Text[response.AnimateFrom!.Value..]);
        var saved = f.Engine.GetConversation(connectedId);
        Assert.Equal(2, f.Engine.GetSnapshot().Communications.Conversations.Count);
        Assert.Equal(2, saved.Messages.Count);
        Assert.Contains(saved.Messages[1].Content, response.Text);
        Assert.Null((await child.Send("command", "show our conversation")).AnimateFrom);
        Assert.False((await child.Send("command", "turn off animation")).Typewriter);
        Assert.True((await child.Send("command", "turn on animation")).Typewriter);
        Assert.Contains("You: typewriter invalid", (await child.Send("command", "typewriter invalid")).Text);
        Assert.Contains("You: step", (await child.Send("command", "step")).Text);
        Assert.True((await child.Send("command", "/close")).Closed);
        Assert.Equal(Grid.Communication.ConversationStatus.Open, f.Engine.GetConversation(saved.Id).Status);
        await using var resumed = await f.Open("resume " + saved.Id);
        Assert.Contains("Resumed", resumed.Greeting.Text);
        Assert.Contains(saved.Messages[1].Content, (await resumed.Send("command", "show our conversation")).Text);
        Assert.Contains("Ended", (await resumed.Send("command", "disconnect")).Text);
        Assert.Equal(Grid.Communication.ConversationStatus.Ended, f.Engine.GetConversation(saved.Id).Status);
        Assert.Equal(0, f.Engine.TickCount); Assert.True(f.Owner.IsMaster);
    }

    [Fact]
    public async Task SeparateConversationWindowsKeepTurnsAndLogoutIndependent()
    {
        await using var f = new Fixture(); await using var first = await f.Open("connect MCP"); await using var second = await f.Open("connect MCP");
        await first.Send("command", "talk first-window-message");
        await second.Send("command", "talk second-window-message");
        Assert.DoesNotContain("second-window-message", (await first.Send("command", "show our conversation")).Text);
        Assert.DoesNotContain("first-window-message", (await second.Send("command", "show our conversation")).Text);
        await first.Send("command", "return to terminal");
        Assert.True((await first.Send("command", "logout")).ConfirmLogout);
        Assert.False((await first.Send("ping")).Closed);
        Assert.True((await first.Send("logout")).Closed);
        Assert.False((await second.Send("ping")).Closed); Assert.True(f.Owner.IsMaster);
        Assert.All(f.Engine.GetSnapshot().Communications.Conversations, c => Assert.Equal(2, c.Messages.Count));
    }

    [Fact]
    public async Task ConversationLaunchFailureAndInvalidTargetDoNotCreateTranscripts()
    {
        await using (var failed = new Fixture(_ => throw new IOException("launch failed")))
        {
            Assert.Contains("could not open", failed.Commands.Execute("connect MCP").Text);
            Assert.Empty(failed.Engine.GetSnapshot().Communications.Conversations);
        }
        await using var f = new Fixture();
        await using var invalid = await f.Open("connect nonexistent-program");
        Assert.True(invalid.Greeting.Closed);
        Assert.Empty(f.Engine.GetSnapshot().Communications.Conversations);
        Assert.Contains("No window opened", f.Commands.Execute("resume invalid").Text);
    }

    [Fact]
    public async Task ConversationRevocationPreventsFurtherTextAndPendingConnection()
    {
        await using var f = new Fixture(); await using var child = await f.Open("connect MCP");
        await child.Send("command", "talk hello");
        f.Owner.Logout();
        var denied = await child.Send("command", "show our conversation");
        Assert.True(denied.Closed); Assert.DoesNotContain("hello", denied.Text);
        f.Owner.Login();
        Assert.Contains("Opened", f.Commands.Execute("connect MCP").Text);
        f.Owner.Logout();
        await using var pending = new Client(f.Endpoint); await pending.Connect();
        Assert.True(pending.Greeting.Closed);
        Assert.Single(f.Engine.GetSnapshot().Communications.Conversations);
    }

    [Fact]
    public async Task AgentConversationCannotResumeOwnersPrivateHistory()
    {
        await using var f = new Fixture(); await using var ownerChat = await f.Open("connect MCP");
        await ownerChat.Send("command", "talk private-owner-message");
        var id = Assert.Single(f.Engine.GetSnapshot().Communications.Conversations).Id;
        f.Engine.StartAgents(f.Owner.Reference);
        var agent = f.Engine.ResolveProgram("JN");
        f.Owner.Switch(agent.Id.ToString());
        await using var refused = await f.Open("resume " + id);
        Assert.True(refused.Greeting.Closed); Assert.DoesNotContain("private-owner-message", refused.Greeting.Text);
        Assert.True((await ownerChat.Send("ping")).Closed);
    }

    [Fact]
    public async Task RealConversationProcessDisplaysInstantRedirectedRepliesWithoutOwningWorld()
    {
        System.Diagnostics.Process? child = null;
        Task<string>? output = null; Task<string>? error = null;
        await using var f = new Fixture(endpoint =>
        {
            var start = new System.Diagnostics.ProcessStartInfo("dotnet") { UseShellExecute = false, CreateNoWindow = true, WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden, RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true };
            start.ArgumentList.Add(typeof(WindowHub).Assembly.Location); start.ArgumentList.Add("--attach"); start.ArgumentList.Add(endpoint); start.ArgumentList.Add("--no-color");
            child = System.Diagnostics.Process.Start(start)!;
            output = child.StandardOutput.ReadToEndAsync(); error = child.StandardError.ReadToEndAsync();
            child.StandardInput.WriteLine("talk status"); child.StandardInput.WriteLine("turn off animation"); child.StandardInput.WriteLine("show our conversation"); child.StandardInput.WriteLine("/close"); child.StandardInput.Close();
            return child.Id;
        });
        try
        {
            Assert.Contains("Opened conversation", f.Commands.Execute("connect MCP").Text);
            await child!.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(20));
            Assert.True(child.ExitCode == 0, await error!);
            var text = await output!;
            Assert.Contains("GRID.AI // CONVERSATION", text); Assert.Contains("VERIFIED GRID.AI STATE", text);
            Assert.Contains("Typewriter off", text); Assert.DoesNotContain("World saved", text);
            Assert.Equal(2, Assert.Single(f.Engine.GetSnapshot().Communications.Conversations).Messages.Count);
            Assert.True(f.Owner.IsMaster); Assert.Equal(0, f.Engine.TickCount);
        }
        finally { if (child is not null) { if (!child.HasExited) child.Kill(); child.Dispose(); } }
    }

    [Fact]
    public async Task ProtocolRejectsOversizedFrames()
    {
        using var stream = new MemoryStream(BitConverter.GetBytes(WindowProtocol.MaximumFrame + 1));
        await Assert.ThrowsAsync<IOException>(() => WindowProtocol.ReadAsync<WindowRequest>(stream, default));
    }

    [Fact]
    public async Task UnexpectedProcessCannotConsumeWindowAttachment()
    {
        if (!OperatingSystem.IsWindows()) return;
        await using var f = new Fixture(_ => int.MaxValue);
        Assert.Contains("Opened", f.Commands.Execute("ntm").Text);
        await using var unexpected = new Client(f.Endpoint);
        await Assert.ThrowsAnyAsync<IOException>(() => unexpected.Connect());
        Assert.True(f.Owner.IsMaster);
    }
    [Fact]
    public async Task RealAttachedProcessUsesHostWithoutOpeningAWorldOrScheduler()
    {
        System.Diagnostics.Process? child = null;
        Task<string>? output = null; Task<string>? error = null;
        await using var f = new Fixture(endpoint =>
        {
            var start = new System.Diagnostics.ProcessStartInfo("dotnet") { UseShellExecute = false, CreateNoWindow = true, WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden, RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true };
            start.ArgumentList.Add(typeof(WindowHub).Assembly.Location); start.ArgumentList.Add("--attach"); start.ArgumentList.Add(endpoint); start.ArgumentList.Add("--no-color");
            child = System.Diagnostics.Process.Start(start)!;
            output = child.StandardOutput.ReadToEndAsync(); error = child.StandardError.ReadToEndAsync();
            child.StandardInput.WriteLine("step"); child.StandardInput.WriteLine("status"); child.StandardInput.WriteLine("/close"); child.StandardInput.Close();
            return child.Id;
        });
        f.Engine.Pause();
        try
        {
            Assert.Contains("Opened", f.Commands.Execute("ntm").Text);
            await child!.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(20));
            Assert.True(child.ExitCode == 0, await error!);
            var text = await output!;
            Assert.Contains("ATTACHED TERMINAL", text); Assert.Contains("Tick: 1", text);
            Assert.DoesNotContain("World saved", text); Assert.Equal(1, f.Engine.TickCount);
            Assert.True(f.Owner.IsMaster);
        }
        finally { if (child is not null) { if (!child.HasExited) child.Kill(); child.Dispose(); } }
    }
    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => Now;
    }
    [Fact]
    public void ExpiryAndCredentialRotationRevokeDelegatedSessions()
    {
        var clock = new Clock(); var identity = new TerminalIdentitySession(() => "owner", clock);
        var engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis()); identity.Attach(engine); identity.Login();
        var child = identity.CreateAttachment(); clock.Now = clock.Now.AddHours(9);
        Assert.False(child.IsAuthenticated); Assert.False(child.Allows("world.admin"));
        identity.Login(); child = identity.CreateAttachment();
        identity.ConfigureCredentials("Owner", "ABC123", "ABC123");
        Assert.False(child.IsAuthenticated); Assert.Contains("denied", child.Login());
    }

    [Fact]
    public async Task OwnerShutdownDisconnectsWindowsBeforeSavingAndRetainsExclusiveWorldLease()
    {
        var directory = Path.Combine(Path.GetTempPath(), "GridWindowSave-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        try
        {
            await using var f = new Fixture(); var path = Path.Combine(directory, "world.db");
            using var store = new Grid.Persistence.WorldStore(path);
            var info = new Grid.Persistence.WorldInfo(f.Engine.GetSnapshot().WorldId, "Window test", DateTimeOffset.UtcNow);
            store.Create(info, f.Engine.GetSnapshot());
            await using var child = await f.Open(); f.Engine.Pause(); await child.Send("command", "step");
            Assert.Throws<IOException>(() => new Grid.Persistence.WorldStore(path));
            var stopped = false;
            async Task Stop() { await f.Hub.StopAsync(); stopped = true; }
            var persistence = new Grid.Persistence.WorldPersistenceSession(store, info, f.Engine);
            var host = new TerminalHost(f.Engine, f.Commands, new StringReader("exit\n"), new StringWriter(), showPrompt: false, persistence: persistence, stopAttachments: Stop);
            Assert.Equal(0, await host.RunAsync()); Assert.True(stopped);
            await Assert.ThrowsAnyAsync<IOException>(() => child.Send("ping"));
            Assert.Equal(1, store.Load().State.TickCount);
        }
        finally { Directory.Delete(directory, true); }
    }
}
