using System.Text;
using System.Text.Json.Nodes;
using Grid.Core;
using Grid.Persistence;
using Grid.Simulation;
using Grid.Simulation.Tasks;
using Xunit;
namespace Grid.Terminal.Tests;

[Trait("Phase", "6.9")]
public sealed class ConversationalFileTests : IDisposable
{
    private readonly string folder = Path.Combine(Path.GetTempPath(), "Grid69-" + Guid.NewGuid().ToString("N"));
    private string Root => Path.Combine(folder, "source");
    private string Documents => Path.Combine(folder, "Documents");
    private string Desktop => Path.Combine(folder, "Desktop");
    private string Artifacts => Path.Combine(folder, "artifacts");
    private readonly TerminalSessionRegistry registry = new();
    private readonly TerminalIdentitySession owner = new(() => "file-owner");
    private readonly GridSimulationEngine engine;
    private readonly CommandDispatcher commands;
    public ConversationalFileTests()
    {
        Directory.CreateDirectory(Root); File.WriteAllText(Path.Combine(Root, "sample.csproj"), "<Project />");
        registry.Add(owner); engine = new(GridWorldFactory.CreateGenesis(), editAuthorizer: registry);
        owner.Attach(engine); owner.Login(); engine.StartAgents(owner.Reference); Directory.CreateDirectory(Desktop); Directory.CreateDirectory(Documents); engine.ConfigureMaxisRuntime(Artifacts, desktopRoot: Desktop, documentsRoot: Documents, folderSearchRoots: [folder]);
        engine.RegisterDevelopmentWorkspace(owner.Reference, "demo", Root, "sample.csproj");
        commands = Create(engine, owner); commands.Execute("connect MCP -inline");
    }
    private static CommandDispatcher Create(GridSimulationEngine engine, TerminalIdentitySession identity)
    {
        var commands = DefaultCommands.Create(engine); WorkspaceChangeCommands.Register(commands, engine, identity);
        ChatCommands.Register(commands, engine, identity); commands.UseIdentity(identity); return commands;
    }
    private CommandResult Say(string input) => commands.ExecuteTokens(["talk", input]);
    private string Source(string name = "note.md", string text = "ORIGINAL")
    { var path = Path.Combine(Root, name); File.WriteAllText(path, text); return path; }
    private string Evidence(Guid id) => Path.Combine(Artifacts, "workspace-edits", id.ToString("N"));
    public void Dispose() => Directory.Delete(folder, true);

    [Theory]
    [InlineData("six random words", 6, "documents")]
    [InlineData("10 random letters", 10, "desktop")]
    public void NamedFoldersAndExactRandomCounts(string request, int count, string folderName)
    {
        var draft = Say("create a file in my " + folderName + " called exact.md and fill it with " + request + ".");
        Assert.True(draft.FileChange is not null, draft.Text); Assert.Null(draft.BrokerOperation);
        var root = folderName == "desktop" ? Desktop : Documents;
        Assert.Equal(Path.Combine(root, "exact.md"), draft.FileChange.AbsolutePath);
        Assert.False(File.Exists(Path.Combine(root, "exact.md")));
        Say("approve this change"); Say("apply this change");
        var lines = File.ReadAllLines(Path.Combine(root, "exact.md")); Assert.Equal(count, lines.Length);
        Assert.All(lines, line => Assert.True(line.All(char.IsAsciiLetterLower)));
        if(folderName == "desktop") Assert.All(lines, line => Assert.Single(line));
    }
    [Theory]
    [InlineData(true)] [InlineData(false)]
    public void ExplicitAbsoluteDestinationsSupportSpacedPaths(bool fullFile)
    {
        var root = Path.Combine(folder, "another folder in a location"); Directory.CreateDirectory(root);
        var path = Path.Combine(root, "named file.md");
        var request = fullFile ? "create file \"" + path + "\" with content hello" :
            "create a file in \"" + root + "\" called named file.md and fill it with six random words.";
        var draft = Say(request); Assert.True(draft.FileChange is not null, draft.Text);
        Assert.Equal(path, draft.FileChange.AbsolutePath); Assert.False(File.Exists(path));
        Say("approve this change"); Say("apply this change"); Assert.True(File.Exists(path));
        Say("undo this change"); Assert.False(File.Exists(path));
    }
    [Theory]
    [InlineData("approve this change")]
    [InlineData("apply this change")]
    [InlineData("review")]
    public void EarlyApprovalPreservesFolderSelectionAndDoesNotBecomeContent(string action)
    {
        var first = Path.Combine(Documents, "UWGB"); var second = Path.Combine(Desktop, "UWGB");
        Directory.CreateDirectory(first); Directory.CreateDirectory(second);
        var folders = Say("create a file in my UWGB folder called gridTest.txt");
        Assert.Contains("1.", folders.Text); Assert.Null(folders.FileChange);
        var early = Say(action);
        Assert.Contains("First choose", early.Text); Assert.Contains(first, early.Text); Assert.Contains(second, early.Text);
        Assert.Null(early.FileChange); Assert.Null(early.BrokerOperation);
        Assert.Contains("complete text", Say("1").Text);
        early = Say(action); Assert.Contains("First provide the file contents", early.Text); Assert.Null(early.FileChange);
        var draft = Say("content: Hello GRID."); Assert.True(draft.FileChange is not null, draft.Text);
        Assert.False(File.Exists(draft.FileChange.AbsolutePath));
        Say("approve this change"); Say("apply this change");
        Assert.Equal("Hello GRID.", File.ReadAllText(draft.FileChange.AbsolutePath));
    }

    [Fact]
    public async Task LongerRevisionRetainsDestinationRequiresFreshApprovalAndUsesFacts()
    {
        var ai = new Model { Reply = "{\"content\":\"<html>First</html>\"}" }; var terminal = WithModel(ai);
        var pending = terminal.Execute("create file long.html in demo with instructions A page about GRID.");
        var first = await pending.BrokerOperation!(default); terminal.Execute("approve high-risk");
        var longHtml = "<html>" + new string('x', 20000) + "</html>";
        ai.Reply = System.Text.Json.JsonSerializer.Serialize(new { content = longHtml });
        pending = terminal.Execute("make a full web page about all the parts of the grid");
        Assert.NotNull(pending.BrokerOperation);
        var revision = await pending.BrokerOperation(default);
        Assert.True(revision.FileChange is not null, revision.Text);
        Assert.Contains("previousDraft", ai.LastPrompt); Assert.Contains("First", ai.LastPrompt);
        Assert.Contains("gridFacts", ai.LastPrompt); Assert.Contains("health percentages", ai.LastPrompt);
        Assert.NotEqual(first.FileChange!.Id, revision.FileChange.Id);
        Assert.Contains("Approval required", terminal.Execute("apply this change").Text);
        terminal.Execute("approve high-risk"); terminal.Execute("apply this change");
        Assert.Equal(longHtml, File.ReadAllText(Path.Combine(Root, "long.html")));
    }
    [Fact]
    public async Task FailedRevisionRetainsOriginalDraftButClearsApproval()
    {
        var ai = new Model(); var terminal = WithModel(ai);
        var pending = terminal.Execute("create file retained.md in demo with instructions Write a draft.");
        var original = await pending.BrokerOperation!(default); terminal.Execute("approve this change");
        ai.Reply = "invalid json";
        pending = terminal.Execute("expand this into a longer document"); var failed = await pending.BrokerOperation!(default);
        Assert.Null(failed.FileChange); Assert.Contains("Previous draft retained", failed.Text);
        Assert.Contains(original.FileChange!.Id.ToString(), terminal.Execute("changes list").Text);
        Assert.Contains("Approval required", terminal.Execute("apply this change").Text);
        Assert.Contains("GENERATED", terminal.Execute("review").Text);
        Assert.False(File.Exists(Path.Combine(Root, "retained.md")));
    }

    [Fact]
    public async Task CombinedHtmlRequestDraftsContentAndTypoRetainsApproval()
    {
        var destination = Path.Combine(Documents, "UWGB"); Directory.CreateDirectory(destination);
        var ai = new Model { Reply = "{\"content\":\"<!doctype html><title>GRID</title><p>Local simulation</p>\"}" };
        var terminal = WithModel(ai);
        var pending = terminal.Execute("create me a index.html in my uwgb folder that talks about the grid and what it does");
        Assert.NotNull(pending.BrokerOperation);
        var draft = await pending.BrokerOperation(default);
        Assert.True(draft.FileChange is not null, draft.Text);
        var path = Path.Combine(destination, "index.html"); Assert.Equal(path, draft.FileChange.AbsolutePath);
        Assert.Contains("<!doctype html>", draft.Text); Assert.Contains(".html", ai.LastPrompt); Assert.False(File.Exists(path));
        Assert.Contains("high-risk", terminal.Execute("approve this change").Text);
        terminal.Execute("approve high-risk");
        foreach (var typo in new[] { "aplly this change", "aplly high-risk change" })
        {
            var response = terminal.Execute(typo); Assert.Contains("retained", response.Text);
            Assert.Null(response.BrokerOperation); Assert.False(File.Exists(path));
        }
        var list = terminal.Execute("changes list");
        Assert.Contains("Change: " + draft.FileChange.Id, list.Text); Assert.Contains("State: Approved", list.Text);
        terminal.Execute("apply this change"); Assert.Contains("<!doctype html>", File.ReadAllText(path));
    }
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task ContentDescriptionDraftsHtmlUnlessExplicitlyLiteral(bool literal)
    {
        var ai = new Model { Reply = "{\"content\":\"<html><body>GRID</body></html>\"}" };
        var terminal = WithModel(ai);
        terminal.Execute("create file description.html in demo");
        const string description = "this should have html to display a page that talks about the grid and what it does.";
        var result = terminal.ExecuteTokens(["talk", (literal ? "content: " : "") + description]);
        if (!literal) { Assert.NotNull(result.BrokerOperation); result = await result.BrokerOperation(default); }
        else Assert.Null(result.BrokerOperation);
        Assert.True(result.FileChange is not null, result.Text);
        Assert.Contains(literal ? description : "<html>", result.Text);
        Assert.Equal(literal ? 0 : 1, ai.Calls);
        Assert.False(File.Exists(Path.Combine(Root, "description.html")));
    }

    [Fact]
    public void UserCreatedFolderNameResolvesDynamically()
    {
        var destination = Path.Combine(Documents, "School", "Homework"); Directory.CreateDirectory(destination);
        var draft = Say("create a file in my Homework folder called dynamic.md and fill it with six random words.");
        Assert.True(draft.FileChange is not null, draft.Text);
        Assert.Equal(Path.Combine(destination, "dynamic.md"), draft.FileChange.AbsolutePath);
        Assert.False(File.Exists(draft.FileChange.AbsolutePath));
        Say("approve this change"); Say("apply this change");
        Assert.Equal(6, File.ReadAllLines(draft.FileChange.AbsolutePath).Length);
    }
    [Fact]
    public void DuplicateFolderNamesRequireSelectionAndRetainContent()
    {
        var first = Path.Combine(Documents, "Homework"); var second = Path.Combine(Desktop, "Homework");
        Directory.CreateDirectory(first); Directory.CreateDirectory(second);
        var choice = Say("create a file in Homework called chosen.md and fill it with six random words.");
        Assert.Null(choice.FileChange); Assert.Contains(first, choice.Text); Assert.Contains(second, choice.Text);
        Assert.Contains("Choose", choice.Text);
        var draft = Say("1"); Assert.True(draft.FileChange is not null, draft.Text);
        Assert.False(File.Exists(draft.FileChange.AbsolutePath));
        Say("approve this change"); Say("apply this change");
        Assert.Equal(6, File.ReadAllLines(draft.FileChange.AbsolutePath).Length);
        Assert.Single(Directory.GetFiles(folder, "chosen.md", SearchOption.AllDirectories));
    }
    [Fact]
    public void NestedFolderReferenceAndLookupLimitsAreExplicit()
    {
        var root = Path.Combine(Documents, "Classes", "Math"); Directory.CreateDirectory(root);
        var draft = Say("create a file in Classes/Math called nested.md and fill it with six random words.");
        Assert.True(draft.FileChange is not null, draft.Text);
        Assert.Equal(Path.Combine(root, "nested.md"), draft.FileChange.AbsolutePath);
        Assert.True(FileFolderLookup.Find("Math", [folder], maximumDirectories: 1).Incomplete);
        var forbidden = Path.Combine(folder, ".git", "SecretNotes"); Directory.CreateDirectory(forbidden);
        Assert.Empty(FileFolderLookup.Find("SecretNotes", [folder]).Paths);
    }

    [Fact]
    public void ExplicitDriveRootResolvesOnlyTheRequestedFile()
    {
        var path = Path.Combine(Root, "drive-relative.md");
        var drive = Path.GetPathRoot(path)!;
        var relative = Path.GetRelativePath(drive, path);
        var draft = commands.ExecuteTokens(["changes", "create", drive, relative, "root selection"]);
        Assert.True(draft.FileChange is not null, draft.Text);
        Assert.Equal(path, draft.FileChange.AbsolutePath);
        Assert.False(File.Exists(path));
        commands.ExecuteTokens(["changes", "approve", draft.FileChange.Id.ToString()]);
        var applied = commands.ExecuteTokens(["changes", "apply", draft.FileChange.Id.ToString()]);
        Assert.True(applied.FileChange?.State == "Applied", applied.Text);
        Assert.Equal("root selection", File.ReadAllText(path));
    }

    [Fact]
    public void UnknownLocationClarifiesAndTypoDoesNotAct()
    {
        var typo = Say("reate a file in my documents called test.md and fill it with six random words.");
        Assert.Contains("Did you mean", typo.Text); Assert.Null(typo.BrokerOperation);
        var result = Say("create a file in my homework called test.md and fill it with six random words.");
        Assert.Contains("absolute folder path", result.Text); Assert.Null(result.FileChange);
        var root = Path.Combine(folder, "homework"); Directory.CreateDirectory(root);
        var draft = Say(root); Assert.True(draft.FileChange is not null, draft.Text);
        Assert.Equal(Path.Combine(root, "test.md"), draft.FileChange.AbsolutePath);
    }
    [Fact]
    public void AbsoluteFolderCannotBypassReservedPaths()
    {
        var root = Path.Combine(folder, ".git"); Directory.CreateDirectory(root);
        var draft = Say("create a file in \"" + root + "\" called test.md and fill it with six random words.");
        Assert.Null(draft.FileChange); Assert.Contains("refused", draft.Text);
        Assert.False(File.Exists(Path.Combine(root, "test.md")));
    }

    [Fact]
    public void DesktopNaturalRequestStagesTenNumbersAndRequiresApproval()
    {
        var draft = Say("create a file on my desktop called test.md and fill it with 10 random numbers.");
        Assert.NotNull(draft.FileChange);
        Assert.Equal(Path.Combine(Desktop, "test.md"), draft.FileChange.AbsolutePath);
        Assert.Null(draft.BrokerOperation);
        Assert.False(File.Exists(Path.Combine(Desktop, "test.md")));
        Assert.Contains("Approval required", Say("apply this change").Text);
        Say("approve this change"); Say("apply this change");
        var values = File.ReadAllLines(Path.Combine(Desktop, "test.md"));
        Assert.Equal(10, values.Length);
        Assert.All(values, value => Assert.InRange(int.Parse(value), 0, 999));
        Assert.False(File.Exists(Path.Combine(Root, "test.md")));
        Assert.Contains(Path.Combine(Desktop, "test.md"), Say("Where did you save the file?").Text);
        Assert.Contains(Path.Combine(Desktop, "test.md"), Say("read file test.md on my desktop").Text);
    }
    [Fact]
    public void DesktopMissingFieldsAndExistingFileAreExplained()
    {
        Assert.Contains("Which file", Say("create a file on my desktop").Text);
        Assert.Contains("complete text", Say("test.md").Text);
        var draft = Say("content: on my desktop");
        Assert.NotNull(draft.FileChange);
        Say("approve this change"); Say("apply this change");
        var duplicate = Say("create file test.md on my desktop with content on my desktop");
        Assert.Null(duplicate.FileChange); Assert.Contains("already exists", duplicate.Text);
        Assert.Contains(Path.Combine(Desktop, "test.md"), duplicate.Text);
        Assert.Equal("on my desktop", File.ReadAllText(Path.Combine(Desktop, "test.md")));
        Assert.Null(Say("creat").BrokerOperation);
        Assert.Contains("do not need to create", Say("creat").Text);
    }
    [Theory]
    [InlineData("../outside.md")]
    [InlineData(".git/private.md")]
    [InlineData("missing/test.md")]
    public void DesktopCannotEscapeItsDestination(string target)
    {
        var result = Say("create file " + target + " on my desktop with content test");
        Assert.Null(result.FileChange);
        Assert.Contains("refused", result.Text);
    }
    [Fact]
    public void DesktopDeleteRecoveryAndPauseRetainExistingGuards()
    {
        File.WriteAllText(Path.Combine(Desktop, "test.md"), "original");
        var draft = Say("delete file test.md on my desktop");
        Assert.Contains("high-risk", Say("approve this change").Text);
        Say("approve high-risk"); Say("apply this change");
        Assert.False(File.Exists(Path.Combine(Desktop, "test.md")));
        commands.Execute("disconnect"); commands.Execute("connect MCP -inline");
        var recovery = Say("recover change " + draft.FileChange!.Id + " in @desktop");
        Assert.True(recovery.FileChange is not null, recovery.Text);
        Say("undo this change");
        Assert.Equal("original", File.ReadAllText(Path.Combine(Desktop, "test.md")));
        Say("update file test.md on my desktop with content changed"); Say("approve this change");
        commands.Execute("grid -pause"); Say("apply this change");
        Assert.Equal("original", File.ReadAllText(Path.Combine(Desktop, "test.md")));
    }

    [Fact]
    public void LocationsComeFromReceiptsAndNeverGuessDesktop()
    {
        var unknown = Say("Where did you save the file?");
        Assert.Null(unknown.BrokerOperation);
        Assert.Contains("No verified", unknown.Text);
        var path = Path.Combine(Root, "location.md");
        var draft = Say("create file location.md in demo with content exact");
        Assert.Equal(path, draft.FileChange!.AbsolutePath);
        Assert.Contains(path, draft.Text);
        Assert.Contains("Not applied", Say("Where is it saved?").Text);
        Say("approve this change");
        Assert.Contains("Not applied", Say("What is the file path?").Text);
        var applied = commands.Execute("changes apply " + draft.FileChange.Id);
        Assert.Contains(path, applied.Text);
        var location = Say("Where did you save the file?");
        Assert.Null(location.BrokerOperation);
        Assert.Contains(path, location.Text);
        Assert.Contains("State: Applied", location.Text);
        Assert.Equal("exact", File.ReadAllText(path));
        Assert.Contains(path, Say("read file location.md in demo").Text);
        Assert.Contains(path, Say("Where is the file?").Text);
        commands.Execute("disconnect"); commands.Execute("connect MCP -inline");
        Assert.Contains("No verified", Say("Where is the file?").Text);
    }

    [Fact]
    public void ConversationOnlyCreateReadUpdateDeleteAndUndoPreservesUnicode()
    {
        var name = "café 世界 note.md"; var path = Path.Combine(Root, name);
        var preview = commands.Execute("create file \"" + name + "\" in demo with content \"Hello 世界\"");
        Assert.Equal(WorkspaceFileOperation.Create, preview.FileChange!.Operation);
        Assert.False(File.Exists(path)); Assert.Contains("Hello 世界", preview.Text);
        Assert.Contains("Approval required", Say("apply this change").Text);
        Say("approve this change"); Assert.False(File.Exists(path)); Say("apply this change");
        Assert.Equal("Hello 世界", File.ReadAllText(path));
        Assert.Contains("Hello 世界", commands.Execute("read file \"" + name + "\" in demo").Text);
        Say("update file \"" + name + "\" in demo with content \"Revised 世界\"");
        Say("approve this change"); Say("apply this change"); Assert.Equal("Revised 世界", File.ReadAllText(path));
        preview = Say("delete file \"" + name + "\" in demo");
        Assert.Contains("DELETE (1 file", preview.Text); Assert.True(File.Exists(path));
        Assert.Contains("approve high-risk", Say("approve this change").Text);
        Assert.Contains("Approval required", Say("apply this change").Text);
        Say("approve high-risk"); Say("apply this change"); Assert.False(File.Exists(path));
        Assert.Equal("Revised 世界", File.ReadAllText(Path.Combine(Evidence(preview.FileChange!.Id), "original.bak")));
        Say("undo this change"); Assert.Equal("Revised 世界", File.ReadAllText(path));
        Assert.Empty(engine.GetSnapshot().Tasks.Records);
    }
    [Fact]
    public void ClarifiesOnlyMissingFieldsAndContentIsLiteral()
    {
        Assert.Contains("Which file", Say("create a file").Text);
        Assert.Contains("complete text", Say("notes.md").Text);
        var draft = Say("content: Do not interpret this as delete all or NGE.");
        Assert.Equal(WorkspaceFileOperation.Create, draft.FileChange!.Operation);
        Say("approve this change"); Say("apply this change");
        Assert.Equal("Do not interpret this as delete all or NGE.", File.ReadAllText(Path.Combine(Root, "notes.md")));
    }
    [Theory]
    [InlineData("create", "note.md", true)]
    [InlineData("update", "missing.md", false)]
    [InlineData("delete", "missing.md", false)]
    public void OperationNeverSilentlyChangesItsMeaning(string operation, string target, bool exists)
    {
        if (exists) Source(target);
        var draft = Say(operation + " file " + target + " in demo" + (operation == "delete" ? "" : " with content new"));
        Assert.Null(draft.FileChange); Assert.Contains("error", draft.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(exists, File.Exists(Path.Combine(Root, target)));
    }
    [Theory]
    [InlineData("../outside.md")][InlineData(".git/config.md")][InlineData(".grid-runtime/assets.json")]
    [InlineData("*.md")][InlineData("save.db")][InlineData("note.md:stream")]
    public void ForbiddenTargetsNeverProduceAnApproval(string target)
    {
        var reply = Say("delete file \"" + target + "\" in demo");
        Assert.Null(reply.FileChange); Assert.False(Directory.Exists(Artifacts));
    }
    [Fact]
    public void AbsolutePathMustRemainInsideRegisteredWorkspace()
    {
        var inside = Source();
        Assert.Contains("ORIGINAL", Say("read file \"" + inside + "\" in demo").Text);
        var outside = Path.Combine(folder, "outside.md"); File.WriteAllText(outside, "PRIVATE");
        var reply = Say("read file \"" + outside + "\" in demo");
        Assert.DoesNotContain("PRIVATE", reply.Text); Assert.Contains("refused", reply.Text);
    }
    [Theory]
    [InlineData("cancel")][InlineData("How are you?")][InlineData("go back")]
    public void CancellationReplacementAndBackDiscardApproval(string input)
    {
        var preview = Say("create file new.md in demo with content NEW"); Say("approve this change");
        Say(input);
        var replay = commands.Execute("changes apply " + preview.FileChange!.Id);
        Assert.DoesNotContain("Applied demo", replay.Text); Assert.False(File.Exists(Path.Combine(Root, "new.md")));
    }
    [Fact]
    public void NewerWorkSurvivesDeleteApprovalAndRollback()
    {
        var path = Source(); var draft = Say("delete file note.md in demo"); Say("approve high-risk");
        File.WriteAllText(path, "EXTERNAL"); Assert.Contains("Target changed", Say("apply this change").Text);
        Assert.Equal("EXTERNAL", File.ReadAllText(path));
        Say("delete file note.md in demo"); Say("approve high-risk"); Say("apply this change");
        File.WriteAllText(path, "NEW FILE"); Assert.Contains("Rollback refused", Say("undo this change").Text);
        Assert.Equal("NEW FILE", File.ReadAllText(path));
    }
    [Fact]
    public void BackupFailureLeavesDeleteTargetUnchanged()
    {
        var path = Source(); Say("delete file note.md in demo"); Say("approve high-risk");
        File.WriteAllText(Artifacts, "blocked directory"); Say("apply this change");
        Assert.Equal("ORIGINAL", File.ReadAllText(path));
    }
    [Theory]
    [InlineData("logout")][InlineData("pause")][InlineData("workspace")][InlineData("agent")]
    public void RevocationPreventsPendingDelete(string kind)
    {
        var path = Source(); Say("delete file note.md in demo"); Say("approve high-risk");
        switch (kind)
        {
            case "logout": owner.Logout(); break;
            case "pause": engine.Pause(); engine.StartAgents(owner.Reference); break;
            case "workspace": engine.RevokeDevelopmentWorkspace(owner.Reference, "demo"); break;
            case "agent": owner.Switch("MAXIS"); break;
        }
        Say("apply this change"); Assert.Equal("ORIGINAL", File.ReadAllText(path));
    }
    [Fact]
    public void RestartRecoversDeleteButNeverRestoresAnUnappliedApproval()
    {
        var path = Source(); var draft = Say("delete file note.md in demo"); Say("approve high-risk"); Say("apply this change");
        var create = Say("create file pending.md in demo with content PENDING"); Say("approve this change");
        using var store = new WorldStore(Path.Combine(folder, "world.db"));
        store.Create(new WorldInfo(engine.GetSnapshot().WorldId, "Recovery", DateTimeOffset.UtcNow), engine.GetSnapshot());
        var sessions = new TerminalSessionRegistry(); var next = new TerminalIdentitySession(() => "file-owner"); sessions.Add(next);
        var restored = GridSimulationEngine.Restore(store.Load().State, editAuthorizer: sessions); restored.ConfigureMaxisRuntime(Artifacts);
        next.Attach(restored); next.Login(); restored.StartAgents(next.Reference);
        var terminal = Create(restored, next); terminal.Execute("connect MCP -inline");
        Assert.DoesNotContain("Applied demo", terminal.Execute("changes apply " + create.FileChange!.Id).Text);
        Assert.False(File.Exists(Path.Combine(Root, "pending.md")));
        Assert.Contains("Recovery loaded", terminal.Execute("recover change " + draft.FileChange!.Id + " in demo").Text);
        Assert.False(File.Exists(path)); terminal.Execute("undo this change"); Assert.Equal("ORIGINAL", File.ReadAllText(path));
        Assert.Contains("RolledBack", File.ReadAllText(Path.Combine(Evidence(draft.FileChange.Id), "report.json")));
    }
    [Theory]
    [InlineData("create")][InlineData("update")][InlineData("delete")]
    public void RecoveryVerifiesBackupHashesAndIncompleteFinalJournal(string operation)
    {
        var path = Path.Combine(Root, "note.md"); if (operation != "create") Source();
        var draft = Say(operation + " file note.md in demo" + (operation == "delete" ? "" : " with content AFTER"));
        Say(operation == "delete" ? "approve high-risk" : "approve this change"); Say("apply this change");
        var id = draft.FileChange!.Id; var report = Path.Combine(Evidence(id), "report.json");
        var json = JsonNode.Parse(File.ReadAllText(report))!; json["Phase"] = "Prepared"; File.WriteAllText(report, json.ToJsonString());
        var recovered = WorkspaceFileChange.Recover(Path.Combine(Artifacts, "workspace-edits"), Root, id);
        Assert.True(recovered.Applied); recovered.Rollback();
        if (operation == "create") Assert.False(File.Exists(path)); else Assert.Equal("ORIGINAL", File.ReadAllText(path));
    }
    [Fact]
    public void CorruptRecoveryAndWrongWorkspaceAreRefused()
    {
        Source(); var draft = Say("delete file note.md in demo"); Say("approve high-risk"); Say("apply this change");
        var other = Path.Combine(folder, "other"); Directory.CreateDirectory(other); File.WriteAllText(Path.Combine(other, "sample.csproj"), "<Project />");
        engine.RegisterDevelopmentWorkspace(owner.Reference, "other", other, "sample.csproj");
        Assert.Contains("does not belong", commands.Execute("changes recover other " + draft.FileChange!.Id).Text);
        File.WriteAllText(Path.Combine(Evidence(draft.FileChange!.Id), "original.bak"), "TAMPERED");
        Assert.Contains("hash mismatch", commands.Execute("changes recover demo " + draft.FileChange.Id).Text);
        Assert.False(File.Exists(Path.Combine(Root, "note.md")));
    }
    [SymlinkFact]
    public void DeleteAndReadRejectReparseTargetsAndPreserveOriginalBytes()
    {
        var path = Source(); var outside = Path.Combine(folder, "outside.md"); File.WriteAllText(outside, "PRIVATE");
        var link = Path.Combine(Root, "link.md"); File.CreateSymbolicLink(link, outside);
        Assert.Null(Say("delete file link.md in demo").FileChange);
        Assert.DoesNotContain("PRIVATE", Say("read file link.md in demo").Text);
        File.Delete(link);
        var bytes = new byte[] {239,187,191}.Concat(Encoding.UTF8.GetBytes("café\r\n")).ToArray(); File.WriteAllBytes(path, bytes);
        Say("delete file note.md in demo"); Say("approve high-risk"); Say("apply this change"); Say("undo this change");
        Assert.Equal(bytes, File.ReadAllBytes(path));
    }
    private sealed class Model : Grid.Communication.ILocalInference
    {
        public string Reply = "{\"content\":\"GENERATED\"}";
        public Action? BeforeReply;
        public int Calls;
        public string LastPrompt = "";
        public Task<Grid.Communication.InferenceReply> GenerateAsync(string prompt, CancellationToken token = default)
        { Calls++; LastPrompt = prompt; BeforeReply?.Invoke(); return Task.FromResult(new Grid.Communication.InferenceReply(true, Reply, "controlled")); }
    }
    private CommandDispatcher WithModel(Model ai)
    {
        var terminal = DefaultCommands.Create(engine); WorkspaceChangeCommands.Register(terminal, engine, owner, ai);
        ChatCommands.Register(terminal, engine, owner, ai); terminal.UseIdentity(owner); terminal.Execute("connect MCP -inline"); return terminal;
    }
    [Fact]
    public async Task DesktopAiDraftAndUpdateUseFileDestination()
    {
        var ai = new Model { Reply = "{\"content\":\"a B c D e F g H i J\"}" };
        var terminal = WithModel(ai);
        var pending = terminal.Execute("create a file on my desktop called test.md and fill it with a short greeting");
        Assert.NotNull(pending.BrokerOperation);
        var draft = await pending.BrokerOperation(default);
        Assert.True(draft.FileChange is not null, draft.Text);
        var path = Path.Combine(Desktop, "test.md");
        Assert.Equal(path, draft.FileChange.AbsolutePath);
        Assert.False(File.Exists(path));
        terminal.Execute("apply this change"); Assert.False(File.Exists(path));
        terminal.Execute("approve this change"); terminal.Execute("apply this change");
        Assert.Equal("a B c D e F g H i J", File.ReadAllText(path));
        Assert.False(File.Exists(Path.Combine(Root, "test.md")));
        ai.Reply = "{\"content\":\"Updated letters\"}";
        pending = terminal.Execute("update file test.md on my desktop with instructions Replace the letters.");
        draft = await pending.BrokerOperation!(default);
        Assert.True(draft.FileChange is not null, draft.Text);
        Assert.Equal("a B c D e F g H i J", File.ReadAllText(path));
        terminal.Execute("approve this change"); terminal.Execute("apply this change");
        Assert.Equal("Updated letters", File.ReadAllText(path));
        Assert.Contains(path, terminal.Execute("Where did you save this file?").Text);
    }
    [Theory]
    [InlineData("{\"content\":\"bad\",\"approved\":true}")]
    [InlineData("{\"content\":\"bad\",\"path\":\"../outside.md\"}")]
    [InlineData("not json")]
    public async Task DesktopAiCannotAuthorizeOrRedirectDraft(string response)
    {
        var terminal = WithModel(new Model { Reply = response });
        var pending = terminal.Execute("create a file on my desktop called test.md and fill it with a short greeting");
        var result = await pending.BrokerOperation!(default);
        Assert.Null(result.FileChange); Assert.False(File.Exists(Path.Combine(Desktop, "test.md")));
    }
    [Fact]
    public async Task DesktopAiCannotReplaceAFileChangedDuringGeneration()
    {
        var path = Path.Combine(Desktop, "test.md"); File.WriteAllText(path, "original");
        var terminal = WithModel(new Model { BeforeReply = () => File.WriteAllText(path, "external") });
        var pending = terminal.Execute("update file test.md on my desktop with instructions Rewrite it.");
        var result = await pending.BrokerOperation!(default);
        Assert.Null(result.FileChange); Assert.Contains("changed during generation", result.Text);
        Assert.Equal("external", File.ReadAllText(path));
    }

    [Fact]
    public async Task AiDraftUsesReviewedContentAndCannotAuthorizeItself()
    {
        var ai = new Model(); var terminal = WithModel(ai);
        var pending = terminal.Execute("create file generated.md in demo with instructions Write a greeting.");
        var draft = await pending.BrokerOperation!(default);
        Assert.Equal("Draft", draft.FileChange!.State); Assert.Contains("GENERATED", draft.Text);
        var path = Path.Combine(Root, "generated.md"); Assert.False(File.Exists(path));
        terminal.Execute("apply this change"); Assert.False(File.Exists(path));
        terminal.Execute("approve this change"); terminal.Execute("apply this change"); Assert.Equal("GENERATED", File.ReadAllText(path));
        terminal.Execute("update file generated.md in demo");
        pending = terminal.Execute("draft: Write an updated greeting."); ai.Reply = "{\"content\":\"UPDATED\"}";
        draft = await pending.BrokerOperation!(default); Assert.Equal("GENERATED", File.ReadAllText(path));
        terminal.Execute("approve this change"); terminal.Execute("apply this change"); Assert.Equal("UPDATED", File.ReadAllText(path));
    }
    [Theory]
    [InlineData("{\"content\":\"bad\",\"approved\":true}")]
    [InlineData("{\"content\":\"bad\",\"path\":\"../outside.md\"}")]
    [InlineData("not json")]
    public async Task AiCannotAddFieldsOrChooseATarget(string json)
    {
        var ai = new Model { Reply = json }; var terminal = WithModel(ai);
        var pending = terminal.Execute("create file generated.md in demo with instructions Write a greeting.");
        var result = await pending.BrokerOperation!(default);
        Assert.Null(result.FileChange); Assert.False(File.Exists(Path.Combine(Root, "generated.md")));
        Assert.Empty(engine.GetSnapshot().Tasks.Records);
    }
    [Fact]
    public async Task ChangedSourceOrCancelledRequestCannotBecomeAnAiDraft()
    {
        var path = Source(); var ai = new Model { BeforeReply = () => File.WriteAllText(path, "EXTERNAL") }; var terminal = WithModel(ai);
        var pending = terminal.Execute("update file note.md in demo with instructions Update the greeting.");
        Assert.Contains("changed during generation", (await pending.BrokerOperation!(default)).Text);
        Assert.Equal("EXTERNAL", File.ReadAllText(path));
        pending = terminal.Execute("create file cancelled.md in demo with instructions Write a greeting."); terminal.Execute("cancel");
        await pending.BrokerOperation!(default); Assert.Equal(1, ai.Calls);
        Assert.False(File.Exists(Path.Combine(Root, "cancelled.md")));
    }
    [Fact]
    public void PlainContentAndExplicitEmptyContentProduceReviewableDrafts()
    {
        Say("create file plain.md in demo"); Assert.NotNull(Say("Hello world").FileChange);
        Say("approve this change"); Say("apply this change"); Assert.Equal("Hello world", File.ReadAllText(Path.Combine(Root, "plain.md")));
        var draft = Say("create file empty.md in demo with content \"\""); Assert.NotNull(draft.FileChange);
        Say("approve this change"); Say("apply this change"); Assert.Equal(0, new FileInfo(Path.Combine(Root, "empty.md")).Length);
    }
    [Fact]
    public void MalformedRecoveryReportIsHelpfulAndDoesNotRestoreFiles()
    {
        Source(); var draft = Say("delete file note.md in demo"); Say("approve high-risk"); Say("apply this change");
        File.WriteAllText(Path.Combine(Evidence(draft.FileChange!.Id), "report.json"), "{}");
        Assert.Contains("Changes error", commands.Execute("changes recover demo " + draft.FileChange.Id).Text);
        Assert.False(File.Exists(Path.Combine(Root, "note.md")));
    }

    [Fact]
    public void ExactIdTalkAliasesShareTheCurrentReviewedChange()
    {
        var draft = Say("create file aliases.md in demo with content EXACT"); var id = draft.FileChange!.Id;
        Assert.Contains("Approved exact", Say("approve edit " + id).Text);
        Say("apply edit " + id); Assert.Equal("EXACT", File.ReadAllText(Path.Combine(Root, "aliases.md")));
        Say("rollback edit " + id); Assert.False(File.Exists(Path.Combine(Root, "aliases.md")));
    }

    [Fact]
    public async Task UnknownAliasClarificationIsImmediateAndFencesPreservePendingRequest()
    {
        var result = commands.Execute("create file grid69-test.md in grid with content Test one.");
        Assert.Contains("Workspace 'grid' is not registered", result.Text); Assert.Contains("demo", result.Text);
        Assert.Null(result.AnimateFrom); var output = new StringWriter();
        await TypewriterRenderer.WriteAsync(output, result.Text, result.AnimateFrom, true,
            () => throw new Exception("Clarification must not poll input"), () => Task.CompletedTask, default,
            _ => throw new Exception("Clarification must not animate"), result.Presentation);
        Assert.Contains("Reply with an alias", output.ToString());
        Assert.Contains("marker ignored", commands.Execute("```text").Text);
        Assert.Contains("marker ignored", commands.Execute("```").Text);
        Assert.True(commands.ConversationConnected!());
        Assert.NotNull(commands.Execute("demo").FileChange);
        Say("approve this change"); Say("apply this change");
        Assert.Equal("Test one.", File.ReadAllText(Path.Combine(Root, "grid69-test.md")));
    }
    [Fact]
    public void DisconnectedRequestGivesReconnectHelpAndTypoNeverGoesToModel()
    {
        commands.Execute("disconnect");
        Assert.Contains("connect MCP -inline", commands.Execute("create file test.md in demo with content Test").Text);
        commands.Execute("connect MCP -inline");
        var result = commands.Execute("reate file test.md in demo with content Test");
        Assert.Contains("Did you mean create file", result.Text); Assert.Null(result.BrokerOperation);
        Assert.False(File.Exists(Path.Combine(Root, "test.md")));
        Assert.Empty(engine.GetSnapshot().Communications.Conversations.SelectMany(c => c.Messages));
    }

}
