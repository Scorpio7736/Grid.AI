using System.IO.Compression;
using System.Text;
using Grid.Communication;
using Grid.Core;
using Grid.Simulation;
using Xunit;
namespace Grid.Terminal.Tests;

public sealed class ContextTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "GridContext-" + Guid.NewGuid().ToString("N"));
    private readonly TerminalSessionRegistry registry = new();
    private readonly TerminalIdentitySession owner = new(() => "context-owner");
    private readonly GridSimulationEngine engine;
    private readonly CommandDispatcher commands;
    private int saves;
    public ContextTests()
    {
        Directory.CreateDirectory(directory); registry.Add(owner);
        engine = new(GridWorldFactory.CreateGenesis(), editAuthorizer: registry); owner.Attach(engine); owner.Login();
        commands = Commands(owner);
    }
    private CommandDispatcher Commands(TerminalIdentitySession identity, bool persistent = true)
    {
        var result = DefaultCommands.Create(engine); ChatCommands.Register(result, engine, identity);
        ContextCommands.Register(result, engine, identity, persistent ? () => saves++ : null); result.UseIdentity(identity); return result;
    }
    private string FileWith(string name, string content) { var path = Path.Combine(directory, name); File.WriteAllText(path, content, new UTF8Encoding(false)); return path; }
    private ContextDocument Import(string path, string retention = "persistent", string? agent = null)
    {
        Assert.Contains("Imported", commands.Execute($"context add \"{path}\" {retention} \"Project One\" {agent}").Text);
        return engine.ContextFor(null, owner.SessionId).Last();
    }
    public void Dispose() => Directory.Delete(directory, true);
    [Fact]
    public void ImportCapturesVersionAndProvenanceWithoutReopeningSource()
    {
        var path = FileWith("readme.md", "First captured version"); var first = Import(path);
        File.WriteAllText(path, "Second captured version"); var second = Import(path); File.Delete(path);
        Assert.NotEqual(first.Id, second.Id); Assert.NotEqual(first.SourceHash, second.SourceHash);
        Assert.Equal(path, first.SourcePath); Assert.Equal("Project One", first.Project); Assert.Equal("utf8", first.Format);
        Assert.Contains("First captured version", commands.Execute("context read " + first.Id).Text);
        Assert.Contains("Second captured version", commands.Execute("context read " + second.Id).Text);
        Assert.Equal(2, saves);
        Assert.Contains("context list", CommandHelp.Complete("context l", commands.Commands));
        Assert.Contains("context read", CommandHelp.Complete("context r", commands.Commands));
    }
    [Fact]
    public void AgentsOnlySeeExplicitGrantsAndRevocationIsImmediate()
    {
        var agent = engine.EnsureCoordinator(); engine.RegisterAgentIdentity(agent.Id);
        var child = owner.CreateAttachment(); registry.Add(child); child.Switch(agent.Id.ToString()); var agentCommands = Commands(child);
        var privateDoc = Import(FileWith("private.txt", "Private content"));
        var shared = Import(FileWith("shared.txt", "Shared content"), agent: agent.Id.ToString());
        Assert.DoesNotContain("private.txt", agentCommands.Execute("context list").Text);
        Assert.Contains("scope", agentCommands.Execute("context read " + privateDoc.Id).Text);
        Assert.Contains("Shared content", agentCommands.Execute("context read " + shared.Id).Text);
        Assert.Contains("denied", agentCommands.Execute($"context add \"{shared.SourcePath}\" temporary x").Text);
        Assert.Contains("denied", agentCommands.Execute("context remove " + shared.Id).Text);
        Assert.Throws<UnauthorizedAccessException>(() => engine.ChangeContext(child.Reference, shared.Id, true));
        commands.Execute("context revoke " + shared.Id);
        Assert.Contains("scope", agentCommands.Execute("context read " + shared.Id).Text);
        Assert.Contains("Shared content", commands.Execute("context read " + shared.Id).Text);
        commands.Execute("context remove " + shared.Id); Assert.Contains("scope", commands.Execute("context read " + shared.Id).Text);
    }
    [Fact]
    public void TemporaryContextIsSessionScopedNeverSavedAndClearsOnLogout()
    {
        var document = Import(FileWith("temporary.cs", "class Example {}"), "temporary");
        Assert.Empty(engine.GetSnapshot().Context.Documents); Assert.Equal(0, saves);
        var child = owner.CreateAttachment(); registry.Add(child);
        Assert.Contains("scope", Commands(child).Execute("context read " + document.Id).Text);
        owner.Logout(); Assert.Empty(engine.ContextFor(null, owner.SessionId));
        Assert.Empty(GridSimulationEngine.Restore(engine.GetSnapshot()).GetSnapshot().Context.Documents);
    }
    [Theory]
    [InlineData("file.pdf", "unsupported")]
    [InlineData("file.txt", "bad\0text")]
    public void UnsupportedOrBinaryFilesAreRejected(string name, string content)
    {
        var path = FileWith(name, content);
        Assert.Contains("Context error", commands.Execute($"context add \"{path}\" persistent x").Text);
        Assert.Empty(engine.GetSnapshot().Context.Documents); Assert.Equal(0, saves);
    }
    [Fact]
    public void OversizeAndInvalidUtf8AreRejectedWithoutTruncation()
    {
        var path = FileWith("large.txt", new string('a', ContextState.MaxSourceBytes + 1));
        Assert.Contains("1 MiB", commands.Execute($"context add \"{path}\" persistent x").Text);
        File.WriteAllBytes(path, new byte[] { 0xff, 0xfe, 0xff });
        Assert.Contains("Context error", commands.Execute($"context add \"{path}\" persistent x").Text);
        File.WriteAllText(path, new string('a', ContextState.MaxTextCharacters + 1));
        Assert.Contains("Context error", commands.Execute($"context add \"{path}\" persistent x").Text);
        Assert.Empty(engine.GetSnapshot().Context.Documents);
    }
    private string Docx(string xml)
    {
        var path = Path.Combine(directory, Guid.NewGuid() + ".docx");
        using var zip = ZipFile.Open(path, ZipArchiveMode.Create);
        using var text = new StreamWriter(zip.CreateEntry("word/document.xml").Open()); text.Write(xml); return path;
    }
    [Fact]
    public void DocxReadsOnlyBodyTextAndRejectsEntitiesOrOversizedInflation()
    {
        var path = Docx("<w:document xmlns:w='http://schemas.openxmlformats.org/wordprocessingml/2006/main'><w:body><w:p><w:r><w:t>Hello document</w:t></w:r></w:p></w:body></w:document>");
        var document = Import(path); Assert.Equal("docx-body", document.Format); Assert.Contains("Hello document", document.Text);
        path = Docx("<!DOCTYPE x [<!ENTITY secret SYSTEM 'file:///outside.txt'>]><x>&secret;</x>");
        Assert.Contains("Context error", commands.Execute($"context add \"{path}\" persistent x").Text);
        path = Docx(new string('a', ContextState.MaxSourceBytes + 1));
        Assert.Contains("too large", commands.Execute($"context add \"{path}\" persistent x").Text);
        Assert.Single(engine.GetSnapshot().Context.Documents);
    }
    [Fact]
    public void EphemeralModeRefusesPersistentAndUnregisteredAgentGrants()
    {
        var path = FileWith("notes.txt", "Notes"); var ephemeral = Commands(owner, false);
        Assert.Contains("saved world", ephemeral.Execute($"context add \"{path}\" persistent x").Text);
        var agent = engine.EnsureCoordinator();
        Assert.Contains("registered Agent", commands.Execute($"context add \"{path}\" persistent x {agent.Id}").Text);
        Assert.Empty(engine.GetSnapshot().Context.Documents);
    }
    [Fact]
    public void DialogueReportsAvailabilityWithoutPersistingContextBody()
    {
        var agent = engine.EnsureCoordinator(); engine.RegisterAgentIdentity(agent.Id);
        Import(FileWith("private-context.md", "Never copy this body into dialogue"), "temporary", agent.Id.ToString());
        commands.Execute("connect ARCHON");
        var reply = commands.Execute("talk context").Text;
        Assert.Contains("1 captured files", reply); Assert.DoesNotContain("Never copy", reply);
        Assert.DoesNotContain("Never copy", commands.Execute("transcript").Text);
        Assert.DoesNotContain(engine.CaptureHistory().Events, e => e.Detail.Contains("Never copy") || e.ContextJson.Contains("Never copy"));
    }
    [Fact]
    public void ImportDeniesRelativeAndNetworkPathsAndCapacityIsBounded()
    {
        Assert.Throws<ArgumentException>(() => LocalContextReader.Read("relative.txt", "x", null));
        Assert.Throws<ArgumentException>(() => LocalContextReader.Read(@"\\server\share\notes.txt", "x", null));
        var doc = LocalContextReader.Read(FileWith("x.txt", "small"), "x", null);
        for (var i = 0; i < ContextState.MaxFiles; i++) engine.AddContext(owner.Reference, doc with { Id = Guid.NewGuid() }, true);
        Assert.Throws<InvalidDataException>(() => engine.AddContext(owner.Reference, doc with { Id = Guid.NewGuid() }, true));
        Assert.Equal(ContextState.MaxFiles, engine.GetSnapshot().Context.Documents.Count);
    }

    [Fact]
    public void TotalTextQuotaRejectsAtomicallyAndSessionChangeRevokesTemporaryGrants()
    {
        var agent = engine.EnsureCoordinator(); engine.RegisterAgentIdentity(agent.Id);
        var doc = LocalContextReader.Read(FileWith("quota.txt", new string('x', ContextState.MaxTextCharacters)), "x", agent.Id);
        for (var i = 0; i < 4; i++) engine.AddContext(owner.Reference, doc with { Id = Guid.NewGuid() }, false);
        Assert.Equal(4, engine.ContextFor(agent.Id, Guid.Empty).Count);
        Assert.Throws<InvalidDataException>(() => engine.AddContext(owner.Reference, doc with { Id = Guid.NewGuid() }, false));
        owner.Switch(agent.Id.ToString());
        Assert.Empty(engine.ContextFor(agent.Id, Guid.Empty)); Assert.Empty(engine.GetSnapshot().Context.Documents);
    }
    [Fact]
    public void FailedImmediateSaveIsReportedWithoutClaimingDurability()
    {
        var path = FileWith("save.txt", "pending save"); var failing = DefaultCommands.Create(engine);
        ContextCommands.Register(failing, engine, owner, () => throw new IOException("disk test failure")); failing.UseIdentity(owner);
        var result = failing.Execute($"context add \"{path}\" persistent x").Text;
        Assert.Contains("disk test failure", result); Assert.Contains("In-memory changes remain", result);
        Assert.DoesNotContain("Persistent context saved", result); Assert.Single(engine.GetSnapshot().Context.Documents);
    }
}
