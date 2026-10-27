using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Grid.Communication;
using Grid.Core;
using Grid.Simulation;
using Grid.Simulation.Tasks;
using Xunit;
namespace Grid.Terminal.Tests;

public sealed class WorkspaceChangeTests : IDisposable
{
    private readonly string folder = Path.Combine(Path.GetTempPath(), "GridChanges-" + Guid.NewGuid().ToString("N"));
    private string Root => Path.Combine(folder, "source");
    private string Artifacts => Path.Combine(folder, "artifacts");
    private readonly TerminalIdentitySession owner = new(() => "change-owner");
    private readonly GridSimulationEngine engine;
    private readonly CommandDispatcher commands;
    private readonly Model ai = new();
    private int saves;
    private sealed class Model : ILocalInference
    {
        public string Reply = JsonSerializer.Serialize(new { content = "AI revised text\n" });
        public string Prompt = "";
        public bool Structured;
        public TaskCompletionSource? Block;
        public TaskCompletionSource Entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public async Task<InferenceReply> GenerateAsync(string prompt, CancellationToken token = default)
        { Prompt = prompt; Entered.TrySetResult(); if (Block is not null) await Block.Task.WaitAsync(token); return new(true, Reply, "test"); }
        public Task<InferenceReply> GenerateStructuredAsync(string prompt, CancellationToken token = default) { Structured = true; return GenerateAsync(prompt, token); }
    }
    public WorkspaceChangeTests()
    {
        Directory.CreateDirectory(Root);
        File.WriteAllText(Path.Combine(Root, "sample.csproj"), "<Project><Target Name=\"Build\"><Message Text=\"CHANGE BUILD PROBE\" Importance=\"high\" /></Target><Target Name=\"VSTest\"><Message Text=\"CHANGE TEST PROBE\" Importance=\"high\" /></Target></Project>");
        var registry = new TerminalSessionRegistry(); registry.Add(owner);
        engine = new(GridWorldFactory.CreateGenesis(), editAuthorizer: registry); owner.Attach(engine); owner.Login();
        engine.StartAgents(owner.Reference); engine.ConfigureMaxisRuntime(Artifacts);
        engine.RegisterDevelopmentWorkspace(owner.Reference, "demo", Root, "sample.csproj");
        commands = DefaultCommands.Create(engine);
        TaskCommands.Register(commands, engine, owner, _ => { saves++; return Task.CompletedTask; });
        WorkspaceChangeCommands.Register(commands, engine, owner, ai); commands.UseIdentity(owner);
    }
    private Guid Context(string content = "Updated text\n", bool grant = false, string project = "demo")
    {
        var id = Guid.NewGuid();
        var doc = new ContextDocument(id, project, Path.Combine(folder, id + ".txt"), new string('A', 64), Encoding.UTF8.GetByteCount(content),
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "utf8", content, grant ? engine.ResolveProgram("MAXIS").Id : null);
        engine.AddContext(owner.Reference, doc, false); return id;
    }
    private CommandResult Run(string value) => commands.Execute(value);
    private async Task<CommandResult> Async(string value)
    { var result = Run(value); return result.BrokerOperation is { } op ? await op(default) : result; }
    private Guid Stage(string path = "notes.md", string content = "Updated text\n")
    {
        var result = Run($"changes stage demo {path} {Context(content)}");
        var match = Regex.Match(result.Text, @"Change ([a-f0-9-]{36})");
        Assert.True(match.Success, result.Text); return Guid.Parse(match.Groups[1].Value);
    }
    public void Dispose() => Directory.Delete(folder, true);
    [Fact]
    public void NewFileRequiresSeparateApprovalAndApplyAndCanBeUndone()
    {
        var id = Stage(); Assert.False(File.Exists(Path.Combine(Root, "notes.md")));
        Assert.Contains("Approval required", Run("changes apply " + id).Text);
        Assert.Contains("Approved exact", Run("changes approve " + id).Text);
        Assert.False(File.Exists(Path.Combine(Root, "notes.md")));
        Assert.Contains("Applied demo/notes.md", Run("changes apply " + id).Text);
        Assert.Equal("Updated text\n", File.ReadAllText(Path.Combine(Root, "notes.md")));
        Assert.Single(Directory.GetFiles(Artifacts, "candidate.bak", SearchOption.AllDirectories));
        Assert.Contains("Rolled back", Run("changes rollback " + id).Text);
        Assert.False(File.Exists(Path.Combine(Root, "notes.md")));
        Assert.Single(Directory.GetFiles(Artifacts, "candidate.bak", SearchOption.AllDirectories));
    }
    [Fact]
    public void ReplacementBacksUpAndRestoresExactBomAndLineEndings()
    {
        var path = Path.Combine(Root, "notes.md");
        var original = new byte[] { 239, 187, 191 }.Concat(Encoding.UTF8.GetBytes("Old\r\n")).ToArray(); File.WriteAllBytes(path, original);
        var id = Stage(content: "New\r\n"); Run("changes approve " + id); Run("changes apply " + id);
        Assert.Equal(original, File.ReadAllBytes(Directory.GetFiles(Artifacts, "original.bak", SearchOption.AllDirectories).Single()));
        Assert.True(File.ReadAllBytes(path).AsSpan().StartsWith(new byte[] { 239, 187, 191 }));
        Run("changes rollback " + id); Assert.Equal(original, File.ReadAllBytes(path));
    }
    [Fact]
    public void HighRiskRequiresExplicitAcknowledgement()
    {
        var id = Stage("example.cs", "class Example {}");
        Assert.Contains("HIGH risk", Run("changes approve " + id).Text);
        Assert.Contains("Approval required", Run("changes apply " + id).Text);
        Assert.Contains("Approved exact", Run("changes approve " + id + " high-risk").Text);
        Assert.Contains("Applied", Run("changes apply " + id).Text);
    }
    [Theory]
    [InlineData("../escape.md")]
    [InlineData("sub/../escape.md")]
    [InlineData("notes.md:stream")]
    [InlineData(".git/config.md")]
    [InlineData("CON.txt")]
    [InlineData("notes.md.")]
    [InlineData("output.exe")]
    [InlineData("obj/cache.txt")]
    [InlineData("missing/notes.md")]
    public void UnsafeOrUnsupportedPathsAreRefused(string path)
    {
        var result = Run($"changes stage demo \"{path}\" {Context()}");
        Assert.DoesNotContain("Change ", result.Text); Assert.False(Directory.Exists(Artifacts));
    }
    [Fact]
    public void ChangedTargetInvalidatesApprovalWithoutOverwritingNewWork()
    {
        var path = Path.Combine(Root, "notes.md"); File.WriteAllText(path, "Before");
        var id = Stage(); Run("changes approve " + id); File.WriteAllText(path, "External");
        Assert.Contains("Target changed", Run("changes apply " + id).Text); Assert.Equal("External", File.ReadAllText(path));
    }
    [Fact]
    public void ChangedTargetAfterApplyCannotBeRolledBack()
    {
        var id = Stage(); Run("changes approve " + id); Run("changes apply " + id);
        File.WriteAllText(Path.Combine(Root, "notes.md"), "New external work");
        Assert.Contains("Rollback refused", Run("changes rollback " + id).Text);
        Assert.Equal("New external work", File.ReadAllText(Path.Combine(Root, "notes.md")));
    }
    [Fact]
    public void ExistingNewFileCannotBeOverwrittenAfterApproval()
    {
        var id = Stage(); Run("changes approve " + id); File.WriteAllText(Path.Combine(Root, "notes.md"), "Other");
        Assert.Contains("Target changed", Run("changes apply " + id).Text); Assert.Equal("Other", File.ReadAllText(Path.Combine(Root, "notes.md")));
    }
    [Theory]
    [InlineData("context")]
    [InlineData("workspace")]
    [InlineData("pause")]
    [InlineData("logout")]
    [InlineData("switch")]
    public void RevocationPreventsApplication(string kind)
    {
        var id = Stage(); Run("changes approve " + id);
        switch (kind)
        {
            case "context": Context("Different"); break;
            case "workspace": engine.RevokeDevelopmentWorkspace(owner.Reference, "demo"); break;
            case "pause": engine.Pause(); break;
            case "logout": owner.Logout(); break;
            case "switch": owner.Switch("MAXIS"); break;
        }
        Run("changes apply " + id); Assert.False(File.Exists(Path.Combine(Root, "notes.md")));
    }
    [Fact]
    public void ApplyAndRollbackCannotReplay()
    {
        var id = Stage(); Run("changes approve " + id); Run("changes apply " + id);
        Assert.DoesNotContain("Applied demo", Run("changes apply " + id).Text);
        Run("changes rollback " + id); Assert.Contains("not-yet-undone", Run("changes rollback " + id).Text);
    }
    [Fact]
    public void BackupsMustSucceedBeforeAnySourceChanges()
    {
        var id = Stage(); Run("changes approve " + id);
        File.WriteAllText(Artifacts, "blocks directory");
        Assert.Contains("error", Run("changes apply " + id).Text, StringComparison.OrdinalIgnoreCase);
        Assert.False(File.Exists(Path.Combine(Root, "notes.md")));
    }
    [Fact]
    public void VerifyQueuesSavedTaskButNeverRunsIt()
    {
        var id = Stage(); Run("changes approve " + id); Run("changes apply " + id);
        Run("changes verify " + id + " build");
        var task = Assert.Single(engine.GetSnapshot().Tasks.Records);
        Assert.Equal("maxis.build", task.Capability); Assert.Equal(Grid.Simulation.Tasks.TaskStatus.AwaitingApproval, task.Status);
        Assert.Equal(0, task.Attempts); Assert.True(saves > 0);
    }
    [Theory]
    [InlineData("build")]
    [InlineData("test")]
    public async Task ApprovedVerificationRunsExistingBoundedRuntime(string kind)
    {
        var id = Stage(); Run("changes approve " + id); Run("changes apply " + id);
        Run("changes verify " + id + " " + kind);
        var task = Assert.Single(engine.GetSnapshot().Tasks.Records);
        engine.ApproveTask(owner.Reference, task.Id);
        await engine.RunNextTaskAsync(owner.Reference, _ => Task.CompletedTask, default);
        var completed = engine.GetSnapshot().Tasks.Records.Single();
        Assert.Equal(Grid.Simulation.Tasks.TaskStatus.Completed, completed.Status);
        Assert.Contains("Process exited 0", completed.Result);
        Assert.Equal("Updated text\n", File.ReadAllText(Path.Combine(Root, "notes.md")));
    }
    [Fact]
    public async Task AiReceivesOnlyExplicitMaxisContextAndCreatesUnapprovedDraft()
    {
        var context = Context("Use a useful heading", true); Context("OWNER_SECRET"); Context("OTHER_PROJECT", true, "other");
        var result = await Async($"changes suggest demo notes.md {context} Write a concise note");
        Assert.True(ai.Structured); Assert.Contains("Use a useful heading", ai.Prompt);
        Assert.DoesNotContain("OWNER_SECRET", ai.Prompt); Assert.DoesNotContain("OTHER_PROJECT", ai.Prompt);
        Assert.Contains("AI revised text", result.Text); Assert.Contains("No change applied", result.Text);
        Assert.False(File.Exists(Path.Combine(Root, "notes.md"))); Assert.Empty(engine.GetSnapshot().Tasks.Records);
    }
    [Fact]
    public async Task OwnerOnlyContextIsNotSentToAi()
    {
        var result = await Async($"changes suggest demo notes.md {Context()} write a note");
        Assert.Contains("MAXIS grant", result.Text); Assert.Equal("", ai.Prompt);
    }
    [Theory]
    [InlineData("{\"content\":\"hi\",\"path\":\"C:/outside.txt\"}")]
    [InlineData("{\"content\":\"hi\",\"content\":\"other\"}")]
    [InlineData("{\"content\":\"\"}")]
    [InlineData("not json")]
    [InlineData("[]")]
    public async Task InvalidAiDraftsDoNotCreateFiles(string response)
    {
        ai.Reply = response;
        var result = await Async($"changes suggest demo notes.md {Context(grant: true)} write");
        Assert.DoesNotContain("Change ", result.Text); Assert.False(File.Exists(Path.Combine(Root, "notes.md")));
    }
    [Theory]
    [InlineData("logout")]
    [InlineData("context")]
    [InlineData("workspace")]
    [InlineData("target")]
    public async Task AiDraftCannotSurviveChangedScopeOrSource(string change)
    {
        ai.Block = new();
        var pending = Async($"changes suggest demo notes.md {Context(grant: true)} write");
        await ai.Entered.Task.WaitAsync(TimeSpan.FromSeconds(2));
        switch (change)
        {
            case "logout": owner.Logout(); break;
            case "context": Context(); break;
            case "workspace": engine.RevokeDevelopmentWorkspace(owner.Reference, "demo"); break;
            case "target": File.WriteAllText(Path.Combine(Root, "notes.md"), "external"); break;
        }
        ai.Block.TrySetResult();
        var result = await pending.WaitAsync(TimeSpan.FromSeconds(3));
        Assert.DoesNotContain("No change applied. Review all content", result.Text);
        Assert.Empty(engine.GetSnapshot().Tasks.Records);
    }
    [Fact]
    public void ContextProjectMustMatchWorkspace()
    {
        var result = Run($"changes stage demo notes.md {Context(project: "elsewhere")}");
        Assert.Contains("missing or ambiguous", result.Text); Assert.False(File.Exists(Path.Combine(Root, "notes.md")));
    }
    [Fact]
    public void ReplacedParentLinkCannotRedirectApprovedWrite()
    {
        var sub=Path.Combine(Root,"sub"); var outside=Path.Combine(folder,"outside");
        Directory.CreateDirectory(sub); Directory.CreateDirectory(outside);
        var id=Stage("sub/notes.md"); Run("changes approve "+id);
        Directory.Delete(sub); Directory.CreateSymbolicLink(sub,outside);
        try
        {
            Assert.Contains("reparse",Run("changes apply "+id).Text);
            Assert.False(File.Exists(Path.Combine(outside,"notes.md")));
        }
        finally { Directory.Delete(sub); }
    }
    [Fact]
    public void PreviewEscapesControlSequences()
    {
        File.WriteAllText(Path.Combine(Root, "notes.md"), "Hello \u001b[31m");
        var id = Stage();
        var preview = Run("changes preview " + id).Text;
        Assert.DoesNotContain('\u001b', preview); Assert.Contains("\\u001B", preview);
    }
}

