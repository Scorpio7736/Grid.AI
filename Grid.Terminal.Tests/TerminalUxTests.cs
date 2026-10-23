using Grid.Core;
using Grid.Communication;
using Grid.Simulation;
using Xunit;
namespace Grid.Terminal.Tests;

public sealed class TerminalUxTests
{
    private sealed class Input(params string?[] lines) : ITerminalInput
    {
        private readonly Queue<string?> values = new(lines);
        public bool Interactive { get; set; } = true;
        public List<bool> SecretReads { get; } = [];
        public Task<string?> ReadAsync(bool secret, CancellationToken token) { token.ThrowIfCancellationRequested(); SecretReads.Add(secret); return Task.FromResult(values.Count == 0 ? null : values.Dequeue()); }
    }
    private static (GridSimulationEngine Engine, TerminalIdentitySession Identity, CommandDispatcher Commands) Fixture()
    {
        var identity = new TerminalIdentitySession(() => "old-machine");
        var engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis(), editAuthorizer: identity); identity.Attach(engine);
        var commands = DefaultCommands.Create(engine); IdentityCommands.Register(commands, engine, identity, interactiveUi: true); commands.UseIdentity(identity);
        return (engine, identity, commands);
    }
    [Fact]
    public async Task SetupLoginWelcomeAndConfirmedLogoutKeepSecretsOutOfOutput()
    {
        var (engine, identity, commands) = Fixture();
        const string password = "test-only-passphrase";
        var input = new Input("Jack", password, password, "Jack", password, "", "n", "invalid", "y");
        var output = new StringWriter(); var clears = 0; var ui = new TerminalSessionUi(identity, input, output, () => { clears++; return true; });
        Assert.True(await ui.StartAsync(default)); Assert.False(identity.IsAuthenticated); Assert.True(identity.UsesPassword);
        Assert.True(await ui.HandleAsync(commands.Execute("login"), default)); Assert.True(identity.IsMaster);
        Assert.Contains("Welcome, Jack", output.ToString()); Assert.Contains("Authority level: Master Control", output.ToString());
        Assert.True(await ui.HandleAsync(commands.Execute("logout"), default)); Assert.True(identity.IsMaster);
        Assert.True(await ui.HandleAsync(commands.Execute("logout"), default)); Assert.False(identity.IsAuthenticated);
        Assert.True(clears >= 3); Assert.DoesNotContain(password, output.ToString());
        Assert.Equal(3, input.SecretReads.Count(s => s));
        Assert.DoesNotContain(engine.CaptureHistory().Events, e => e.Detail.Contains(password) || e.ContextJson.Contains(password));
    }
    [Fact]
    public async Task RedirectedSecretsRefusedAndEofCancelsLogout()
    {
        var (_, identity, commands) = Fixture(); var output = new StringWriter();
        var redirected = new Input { Interactive = false }; var ui = new TerminalSessionUi(identity, redirected, output);
        await ui.StartAsync(default); await ui.HandleAsync(commands.Execute("login"), default);
        Assert.False(identity.IsAuthenticated); Assert.Empty(redirected.SecretReads); Assert.Contains("refused", output.ToString());
        identity.Login(); Assert.True(identity.IsMaster);
        var eof = new TerminalSessionUi(identity, new Input(), output);
        Assert.False(await eof.HandleAsync(commands.Execute("logout"), default)); Assert.True(identity.IsMaster);
    }
    [Fact]
    public void PasswordMigrationNeedsOwnerAndDisablesSidBypassAcrossMachines()
    {
        var (engine, identity, _) = Fixture(); identity.Login(); identity.Logout();
        Assert.Contains("denied", identity.ConfigureCredentials("Jack", "test-only-passphrase", "test-only-passphrase"));
        identity.Login(); var ownerId = engine.LocalOperator.Id;
        var archon = engine.CreateRunningCoordinator(); engine.RegisterAgentIdentity(archon.Id);
        identity.ConfigureCredentials("Jack", "test-only-passphrase", "test-only-passphrase");
        Assert.Contains("denied", identity.Login()); Assert.False(identity.IsAuthenticated);
        var migrated = new TerminalIdentitySession(() => "different-machine");
        var restored = GridSimulationEngine.Restore(engine.GetSnapshot(), editAuthorizer: migrated); migrated.Attach(restored);
        Assert.Contains("Logged in", migrated.LoginPassword("jack", "test-only-passphrase"));
        Assert.Equal(ownerId, restored.LocalOperator.Id); Assert.Contains(archon.Id, restored.GetSnapshot().Identities.RegisteredAgents);
        Assert.Contains("Active agent", migrated.Switch(archon.Id.ToString())); Assert.False(migrated.IsMaster);
    }
    [Fact]
    public void CredentialHashUsesSaltAndFailedPasswordsCannotAuthenticate()
    {
        var one = PasswordCredential.Create("User", "test-only-passphrase"); var two = PasswordCredential.Create("User", "test-only-passphrase");
        Assert.NotEqual(one.Hash, two.Hash); Assert.NotEqual(one.Salt, two.Salt);
        Assert.False(one.Verify("Other", "test-only-passphrase")); Assert.False(one.Verify("User", "incorrect"));
        Assert.Throws<ArgumentException>(() => PasswordCredential.Create("User", "short"));
        var (_, identity, _) = Fixture(); identity.ConfigureCredentials("User", "test-only-passphrase", "test-only-passphrase");
        for (var i = 0; i < 5; i++) Assert.Contains("denied", identity.LoginPassword("User", "incorrect"));
        Assert.Contains("Wait 30 seconds", identity.LoginPassword("User", "test-only-passphrase"));
    }
    [Fact]
    public void HelpCompletionAndPaletteFollowCapabilitiesAndNeverSuggestSecrets()
    {
        var (_, identity, commands) = Fixture();
        var help = commands.Execute("help").Text; Assert.Contains("    - help -md", help); Assert.Contains("# USAGE:", help); Assert.Contains("# EXAMPLE:", help);
        Assert.Empty(CommandHelp.Complete("ed", commands.Commands)); Assert.Contains("login", CommandHelp.Complete("lo", commands.Commands));
        identity.Login();
        var manual = commands.Execute("help -md").Text;
        foreach (var command in commands.Commands) Assert.Contains(command.Name, manual);
        Assert.Contains("edit", CommandHelp.Complete("ed", commands.Commands));
        Assert.Contains("programs -active", CommandHelp.Complete("programs -a", commands.Commands));
        Assert.Empty(CommandHelp.Complete("edit \"Secret", commands.Commands));
        identity.Logout(); Assert.Empty(CommandHelp.Complete("ed", commands.Commands));
        Assert.Equal(ConsoleColor.Yellow, TerminalTheme.Color("Name | Value")); Assert.Equal(ConsoleColor.Yellow, TerminalTheme.Color("---+---"));
        Assert.Equal(ConsoleColor.Red, TerminalTheme.Color("Access denied")); Assert.Equal(ConsoleColor.Cyan, TerminalTheme.Color("Welcome"));
        Assert.Contains("GRID7736", TerminalSessionUi.Opening(30));
        Assert.Contains("OFFLINE MARKDOWN HELP", commands.Execute("help -md").Text);
        Assert.Contains("# GRID7736", commands.Execute("help -md").Text);
    }

    [Theory]
    [InlineData("abcdef")]
    [InlineData("ABCDEF")]
    [InlineData("123456")]
    [InlineData("!@#$%^")]
    [InlineData("Ab1!xy")]
    [InlineData("abcdefghijklmnopqrstuvwx")]
    [InlineData(" Ab1! ")]
    public void PasswordPolicyAcceptsAllowedCharactersWithoutMandatoryMixture(string password)
    {
        var credential = PasswordCredential.Create("Owner", password);
        Assert.True(credential.Verify("Owner", password));
        Assert.False(credential.Verify("Owner", password + "x"));
        if (password.Trim() != password) Assert.False(credential.Verify("Owner", password.Trim()));
        if (password.ToLowerInvariant() != password) Assert.False(credential.Verify("Owner", password.ToLowerInvariant()));
    }
    [Theory]
    [InlineData("abcde")]
    [InlineData("abcdefghijklmnopqrstuvwxy")]
    [InlineData("abc\ndef")]
    public void PasswordPolicyRejectsInvalidNewPasswords(string password)
    {
        var error = Assert.Throws<ArgumentException>(() => PasswordCredential.Create("Owner", password));
        Assert.Contains("6..24", error.Message);
    }
    [Fact]
    public async Task SixCharacterSetupDisplaysNewPolicyAndMismatchDoesNotCreateCredential()
    {
        var (_, identity, commands) = Fixture(); var output = new StringWriter();
        var ui = new TerminalSessionUi(identity, new Input("Owner", "Ab1!xy", "Ab1!xz", "Owner", "Ab1!xy", "Ab1!xy"), output);
        await ui.StartAsync(default);
        Assert.True(identity.NeedsSetup); Assert.Contains("passwords do not match", output.ToString());
        await ui.HandleAsync(commands.Execute("login"), default);
        Assert.True(identity.UsesPassword); Assert.Contains("Password: 6..24", output.ToString());
        Assert.Contains("Logged in", identity.LoginPassword("Owner", "Ab1!xy"));
        Assert.DoesNotContain("Ab1!xy", output.ToString());
    }
    [Theory]
    [InlineData(25)]
    [InlineData(256)]
    public void ExistingLongPasswordSurvivesRestoreAndCanBeRotated(int length)
    {
        var (engine, identity, _) = Fixture();
        var password = new string('A', length);
        var salt = System.Security.Cryptography.RandomNumberGenerator.GetBytes(16);
        var hash = System.Security.Cryptography.Rfc2898DeriveBytes.Pbkdf2(password, salt, PasswordCredential.WorkFactor, System.Security.Cryptography.HashAlgorithmName.SHA256, 32);
        engine.SetLocalCredential(new("Owner", Convert.ToBase64String(salt), Convert.ToBase64String(hash), PasswordCredential.WorkFactor));
        var restoredIdentity = new TerminalIdentitySession(() => "another-machine");
        var restored = GridSimulationEngine.Restore(engine.GetSnapshot(), editAuthorizer: restoredIdentity); restoredIdentity.Attach(restored);
        Assert.Contains("Logged in", restoredIdentity.LoginPassword("Owner", password));
        Assert.Throws<ArgumentException>(() => restoredIdentity.ConfigureCredentials("Owner", password, password));
        Assert.True(restoredIdentity.IsMaster);
        Assert.Contains("saved", restoredIdentity.ConfigureCredentials("Owner", "Ab1!xy", "Ab1!xy"));
        Assert.False(restoredIdentity.IsAuthenticated);
        Assert.Contains("denied", restoredIdentity.LoginPassword("Owner", password));
        Assert.Contains("Logged in", restoredIdentity.LoginPassword("Owner", "Ab1!xy"));
    }
}
