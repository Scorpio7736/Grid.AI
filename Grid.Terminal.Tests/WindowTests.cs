using System.IO.Pipes;
using Grid.Core;
using Grid.Simulation;
using Xunit;
namespace Grid.Terminal.Tests;

public sealed class WindowTests
{
    private sealed class Fixture : IAsyncDisposable
    {
        public readonly TerminalSessionRegistry Registry = new();
        public readonly TerminalIdentitySession Owner;
        public readonly GridSimulationEngine Engine;
        public readonly WindowHub Hub;
        public readonly CommandDispatcher Commands;
        public string Endpoint = "";
        public Fixture(Func<string, int>? launch = null)
        {
            Owner = new(() => "test-owner"); Registry.Add(Owner);
            Engine = new(GridWorldFactory.CreateGenesis(), editAuthorizer: Registry); Owner.Attach(Engine); Owner.Login();
            CommandDispatcher Create(TerminalIdentitySession identity)
            {
                var commands = DefaultCommands.Create(Engine); ChatCommands.Register(commands, Engine, identity);
                IdentityCommands.Register(commands, Engine, identity, interactiveUi: true);
                ContextCommands.Register(commands, Engine, identity);
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
        await first.Send("command", "step");
        Assert.Equal(1, f.Engine.TickCount);
        Assert.Contains("Tick: 1", (await second.Send("command", "status")).Text);
        Assert.True((await first.Send("command", "exit")).Closed);
        await second.Send("command", "step"); Assert.Equal(2, f.Engine.TickCount);
        Assert.Contains("Tick: 2", f.Commands.Execute("status").Text);
    }
    [Fact]
    public async Task ChatAndEditPreviewsAreIndependentButAuthorizedEditsReachSharedEngine()
    {
        await using var f = new Fixture(); await using var first = await f.Open(); await using var second = await f.Open();
        Assert.Contains("Opened conversation", (await first.Send("command", "connect")).Text);
        await using var conversation = new Client(f.Endpoint); await conversation.Connect();
        Assert.Contains("No conversation", (await second.Send("command", "talk hello")).Text);
        Assert.Contains("No conversation", f.Commands.Execute("talk hello").Text);
        var preview = await first.Send("command", "edit MCP occupation WindowTest");
        var id = System.Text.RegularExpressions.Regex.Match(preview.Text, @"Preview ([0-9a-f-]+)").Groups[1].Value;
        Assert.NotEmpty(id);
        Assert.Contains("No matching preview", (await second.Send("command", "apply-edit " + id)).Text);
        Assert.Contains("Applied", (await first.Send("command", "apply-edit " + id)).Text);
        Assert.Equal("WindowTest", f.Engine.GetSnapshot().Programs.Single(p => p.Name == "MCP").Occupation);
    }
    [Fact]
    public async Task AgentAttachmentCannotPromoteOrMutateAndParentLogoutRevokesIt()
    {
        await using var f = new Fixture(); var agent = f.Engine.EnsureCoordinator(); f.Engine.RegisterAgentIdentity(agent.Id); f.Owner.Switch(agent.Id.ToString());
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
        f.Commands.Execute("step");
        Assert.Contains("read-only", (await viewer.Send("command", "step")).Text);
        Assert.Equal(1, f.Engine.TickCount); Assert.Equal("", (await viewer.Send("ping")).Text);
    }
    [Fact]
    public async Task LiveFramesAreSharedAndRevokedOnRoleChange()
    {
        await using var f = new Fixture(); await using var viewer = await f.Open("live -opninwin");
        var frame = await viewer.Send("frame"); Assert.True(frame.Live); Assert.NotEmpty(frame.Text);
        var agent = f.Engine.EnsureCoordinator(); f.Engine.RegisterAgentIdentity(agent.Id); f.Owner.Switch(agent.Id.ToString());
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
        Assert.Contains("status -opninwin", f.Commands.Execute("help").Text);
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
    public async Task DedicatedConversationStoresCompleteTurnsAndClosePreservesResume()
    {
        await using var f = new Fixture();
        await using var child = await f.Open("connect MCP");
        Assert.Equal("conversation", child.Greeting.Mode);
        Assert.Contains("Connected to MCP", child.Greeting.Text);
        Assert.DoesNotContain(child.Greeting.Commands!, c => c.Name is "step" or "voice" or "switch");
        Assert.Contains("No conversation", f.Commands.Execute("talk hello").Text);
        var response = await child.Send("command", "talk status");
        Assert.NotNull(response.AnimateFrom);
        Assert.Contains("MCP", response.Text[response.AnimateFrom!.Value..]);
        var saved = Assert.Single(f.Engine.GetSnapshot().Communications.Conversations);
        Assert.Equal(2, saved.Messages.Count);
        Assert.Contains(saved.Messages[1].Content, response.Text);
        Assert.Null((await child.Send("command", "transcript")).AnimateFrom);
        Assert.False((await child.Send("command", "typewriter off")).Typewriter);
        Assert.True((await child.Send("command", "typewriter on")).Typewriter);
        Assert.Contains("Usage", (await child.Send("command", "typewriter invalid")).Text);
        Assert.Contains("Unknown", (await child.Send("command", "step")).Text);
        Assert.True((await child.Send("command", "exit")).Closed);
        Assert.Equal(Grid.Communication.ConversationStatus.Open, f.Engine.GetConversation(saved.Id).Status);
        await using var resumed = await f.Open("resume " + saved.Id);
        Assert.Contains("Resumed", resumed.Greeting.Text);
        Assert.Contains(saved.Messages[1].Content, (await resumed.Send("command", "transcript")).Text);
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
        Assert.DoesNotContain("second-window-message", (await first.Send("command", "transcript")).Text);
        Assert.DoesNotContain("first-window-message", (await second.Send("command", "transcript")).Text);
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
        await using var invalidResume = await f.Open("resume invalid");
        Assert.True(invalidResume.Greeting.Closed);
        Assert.Contains("Usage", invalidResume.Greeting.Text);
    }

    [Fact]
    public async Task ConversationRevocationPreventsFurtherTextAndPendingConnection()
    {
        await using var f = new Fixture(); await using var child = await f.Open("connect MCP");
        await child.Send("command", "talk hello");
        f.Owner.Logout();
        var denied = await child.Send("command", "transcript");
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
        f.Engine.InitializeSystemAgents(f.Owner.Reference);
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
            child.StandardInput.WriteLine("talk status"); child.StandardInput.WriteLine("typewriter off"); child.StandardInput.WriteLine("transcript"); child.StandardInput.WriteLine("exit"); child.StandardInput.Close();
            return child.Id;
        });
        try
        {
            Assert.Contains("Opened conversation", f.Commands.Execute("connect MCP").Text);
            await child!.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(20));
            Assert.True(child.ExitCode == 0, await error!);
            var text = await output!;
            Assert.Contains("ATTACHED CONVERSATION", text); Assert.Contains("VERIFIED GRID STATE", text);
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
            child.StandardInput.WriteLine("step"); child.StandardInput.WriteLine("status"); child.StandardInput.WriteLine("exit"); child.StandardInput.Close();
            return child.Id;
        });
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
            await using var child = await f.Open(); await child.Send("command", "step");
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
