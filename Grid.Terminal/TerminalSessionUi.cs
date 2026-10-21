namespace Grid.Terminal;

/// <summary>Interactive authentication and welcome screens; keeps secrets outside command dispatch and history.</summary>
public sealed class TerminalSessionUi(TerminalIdentitySession identity, ITerminalInput input, TextWriter output, Func<bool>? clear = null)
{
    private bool wasAuthenticated;
    public ITerminalInput Input => input;
    public static string Opening(int width = 80) => width < 58 ? "GRID7736 // WELCOME" : """
        +======================================================+
        |                                                      |
        |                    G R I D 7 7 3 6                   |
        |                    ENTER THE GRID                    |
        |                                                      |
        +======================================================+
        """;
    private void Clear() { try { if (input.Interactive) clear?.Invoke(); } catch (Exception e) when (e is IOException or InvalidOperationException or PlatformNotSupportedException) { } }
    public async Task<bool> StartAsync(CancellationToken token)
    {
        await Welcome();
        if (identity.NeedsSetup && input.Interactive) return await Setup(token);
        return true;
    }
    private async Task Welcome()
    {
        Clear(); await output.WriteLineAsync(Opening(TerminalTable.Width));
        await output.WriteLineAsync(identity.NeedsSetup ? "Welcome. Local username/password setup is required. Use login in an interactive terminal." :
            identity.UsesPassword ? "Welcome. Use login for local username/password authentication." : "Welcome. Use login master for Windows-owner authentication; credentials migrates after login.");
    }
    public async Task RefreshAsync()
    {
        identity.RevokeExpired();
        if (wasAuthenticated && !identity.IsAuthenticated) { wasAuthenticated = false; await Welcome(); }
    }
    private async Task<string?> Read(string prompt, bool secret, CancellationToken token)
    {
        await output.WriteAsync(prompt); await output.FlushAsync(); return await input.ReadAsync(secret, token);
    }
    private async Task<bool> Setup(CancellationToken token)
    {
        if (!input.Interactive) { await output.WriteLineAsync("Error: credential setup requires interactive masked input; redirected setup is refused."); return true; }
        if (!identity.NeedsSetup && !identity.IsMaster) { await output.WriteLineAsync("Access denied. Authenticate the current owner before credential migration."); return true; }
        await output.WriteLineAsync($"Create local credentials. Existing owner/agent identities and transcripts are preserved. Password: {Grid.Communication.PasswordCredential.MinimumPasswordLength}..{Grid.Communication.PasswordCredential.MaximumPasswordLength} characters. Uppercase, lowercase, numbers and symbols are allowed; no mixture is required.");
        var name = await Read("Username: ", false, token); if (name is null) return false;
        var password = await Read("Password: ", true, token); if (password is null) return false;
        var confirm = await Read("Confirm password: ", true, token); if (confirm is null) return false;
        try { await output.WriteLineAsync(identity.ConfigureCredentials(name, password, confirm)); }
        catch (ArgumentException e) { await output.WriteLineAsync("Error: " + e.Message); }
        finally { password = ""; confirm = ""; } // No secret history/completion/logging; managed strings remain GC-owned.
        await RefreshAsync(); return true;
    }
    public async Task<bool> HandleAsync(CommandResult result, CancellationToken token)
    {
        if (result.SetupRequested) return await Setup(token);
        if (result.LoginRequested)
        {
            if (identity.IsAuthenticated) { await output.WriteLineAsync("Already logged in. Use whoami, switch, or logout."); return true; }
            if (identity.NeedsSetup) return await Setup(token);
            string response;
            if (identity.UsesPassword)
            {
                if (!input.Interactive) { await output.WriteLineAsync("Error: password login requires interactive masked input; redirected authentication is refused."); return true; }
                var user = await Read("Username: ", false, token); if (user is null) return false;
                var password = await Read("Password: ", true, token); if (password is null) return false;
                try { response = identity.LoginPassword(user, password); } finally { password = ""; }
            }
            else response = identity.Login();
            await output.WriteLineAsync(response);
            if (identity.IsAuthenticated)
            {
                wasAuthenticated = true; Clear();
                await output.WriteLineAsync($"Welcome, {TerminalTable.Safe(identity.DisplayName)}\nAuthority level: Master Control\nNo Windows elevation is granted.");
                if (input.Interactive)
                {
                    while (true) { var line = await Read("Press Enter to continue to terminal: ", false, token); if (line is null) return false; if (line.Length == 0) break; }
                }
            }
        }
        if (result.LogoutRequested)
        {
            if (!identity.IsAuthenticated) { await Welcome(); return true; }
            if (!input.Interactive) { await output.WriteLineAsync("Logout confirmation requires interactive input; logout cancelled. EOF/exit still shuts down normally."); return true; }
            while (true)
            {
                var answer = await Read("Log out? [y/N] ", false, token);
                if (answer is null) return false;
                if (string.IsNullOrWhiteSpace(answer) || answer.Trim().Equals("n", StringComparison.OrdinalIgnoreCase)) { await output.WriteLineAsync("Logout cancelled."); break; }
                if (answer.Trim().Equals("y", StringComparison.OrdinalIgnoreCase)) { await output.WriteLineAsync(identity.Logout()); wasAuthenticated = false; await Welcome(); break; }
                await output.WriteLineAsync("Enter y to log out or n to remain logged in.");
            }
        }
        return true;
    }
}
