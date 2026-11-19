using System.Text.Json;
using Grid.Core;
using Grid.Simulation;
using Xunit;

namespace Grid.Terminal.Tests;

public sealed class LocalSkillTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "grid-skills-" + Guid.NewGuid());
    private string Source => Path.Combine(root, "skill.json");
    private string Catalog => Path.Combine(root, "catalog.json");
    private LocalSkillManifest Manifest(string revision = "1") => new(1, "health", revision, "local-test", "MIT", "Status fixture", ["world.status", "resources.status"]);
    public LocalSkillTests() { Directory.CreateDirectory(root); Write(Manifest()); }
    public void Dispose() => Directory.Delete(root, true);
    private void Write(LocalSkillManifest manifest) => File.WriteAllText(Source, JsonSerializer.Serialize(manifest));
    private LocalSkills Installed()
    {
        var catalog = new LocalSkills(Catalog, "owner-device-world");
        catalog.Install(Source, LocalSkills.Inspect(Source).SourceHash); return catalog;
    }
    [Fact]
    public void InstallEnableRunDisableUpdateRollbackAndRemoveSurviveRestart()
    {
        var catalog = Installed();
        Assert.Throws<UnauthorizedAccessException>(() => catalog.Run("health", s => s));
        catalog.Control("health", "enable");
        catalog = new(Catalog, "owner-device-world");
        Assert.Contains("resources.status", catalog.Run("health", s => s));
        Assert.Equal("completed", catalog.Read().Receipts.Last().Outcome);
        catalog.Control("health", "disable");
        Assert.Throws<UnauthorizedAccessException>(() => catalog.Run("health", s => s));
        Write(Manifest("2")); catalog.Install(Source, LocalSkills.Inspect(Source).SourceHash);
        Assert.False(catalog.Read().Skills.Single().Enabled);
        catalog.Control("health", "rollback");
        Assert.Equal("1", catalog.Read().Skills.Single().Manifest.Revision);
        Assert.False(catalog.Read().Skills.Single().Enabled);
        catalog.Control("health", "remove"); Assert.Empty(catalog.Read().Skills);
        Assert.NotEmpty(catalog.Read().Receipts);
    }
    [Fact]
    public void ChangedFileAndChangedDeviceOrOwnerCannotReuseApproval()
    {
        var hash = LocalSkills.Inspect(Source).SourceHash; Write(Manifest("2"));
        var catalog = new LocalSkills(Catalog, "one");
        Assert.Throws<InvalidDataException>(() => catalog.Install(Source, hash));
        catalog.Install(Source, LocalSkills.Inspect(Source).SourceHash); catalog.Control("health", "enable");
        var copied = new LocalSkills(Catalog, "two");
        Assert.False(copied.Read().Skills.Single().Enabled);
        Assert.Throws<UnauthorizedAccessException>(() => copied.Run("health", _ => "private"));
    }
    [Theory]
    [InlineData("shell.exec")]
    [InlineData("world.status; exit")]
    [InlineData("https://example.com")]
    [InlineData("../private")]
    public void ExecutableOrPathStepsAreRejected(string step)
    {
        Write(Manifest() with { Steps = [step] });
        Assert.Throws<InvalidDataException>(() => LocalSkills.Inspect(Source));
    }
    [Fact]
    public void IncompatibleUnknownOversizedAndTamperedManifestsAreRejected()
    {
        Write(Manifest() with { Version = 99 }); Assert.Throws<InvalidDataException>(() => LocalSkills.Inspect(Source));
        Write(Manifest()); File.WriteAllText(Source, File.ReadAllText(Source).TrimEnd('}') + ",\"Script\":\"evil\"}");
        Assert.Throws<JsonException>(() => LocalSkills.Inspect(Source));
        File.WriteAllText(Source, new string('x', 17000)); Assert.Throws<InvalidDataException>(() => LocalSkills.Inspect(Source));
        Write(Manifest()); var catalog = Installed();
        File.WriteAllText(Catalog, File.ReadAllText(Catalog).Replace("Status fixture", "Modified"));
        Assert.Throws<InvalidDataException>(() => catalog.Read());
    }
    [Fact]
    public void FailuresProduceBoundedContentFreeReceiptsAndDoNotReplay()
    {
        var catalog = Installed(); catalog.Control("health", "enable"); var calls = 0;
        Assert.Throws<IOException>(() => catalog.Run("health", _ => { calls++; throw new IOException("secret"); }));
        Assert.Equal(1, calls); Assert.Equal("failed", catalog.Read().Receipts.Last().Outcome);
        Assert.DoesNotContain("secret", File.ReadAllText(Catalog));
        for (var i = 0; i < 110; i++) catalog.Control("health", "disable");
        Assert.Equal(100, catalog.Read().Receipts.Length);
        _ = new LocalSkills(Catalog, "owner-device-world").Read(); Assert.Equal(1, calls);
    }
    [Fact]
    public void TerminalReviewIsSessionBoundSingleUseAndEnableRequiresCurrentHash()
    {
        var owner = new TerminalIdentitySession(() => "test-owner");
        var registry = new TerminalSessionRegistry(); registry.Add(owner);
        var engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis(), editAuthorizer: registry);
        owner.Attach(engine); owner.Login();
        var commands = DefaultCommands.Create(engine); ResourcesCommand.Register(commands, engine);
        SkillCommands.Register(commands, engine, owner, Catalog); commands.UseIdentity(owner);
        string ReviewId() => commands.ExecuteTokens(["skills", "review", Source]).Text.Split("skills install ").Last();
        var id = ReviewId(); owner.Logout(); owner.Login();
        Assert.Contains("invalidated", commands.ExecuteTokens(["skills", "install", id]).Text);
        id = ReviewId(); Assert.Contains("Installed disabled", commands.ExecuteTokens(["skills", "install", id]).Text);
        Assert.Contains("invalidated", commands.ExecuteTokens(["skills", "install", id]).Text);
        Assert.Contains("refused", commands.ExecuteTokens(["skills", "enable", "health", "forged"]).Text);
        var show = commands.ExecuteTokens(["skills", "show", "health"]).Text;
        Assert.Contains("Enabled for manual", commands.Execute(show.Split('\n').Last().Replace("Enable this revision: ", "")).Text);
        Assert.Contains("Tick:", commands.Execute("skills run health").Text);
        var attached = owner.CreateAttachment(); var other = DefaultCommands.Create(engine);
        SkillCommands.Register(other, engine, attached, Catalog); other.UseIdentity(attached);
        Assert.Contains("original Master", other.Execute("skills run health").Text);
        owner.Logout(); Assert.DoesNotContain("Tick:", commands.Execute("skills run health").Text);
    }
}
