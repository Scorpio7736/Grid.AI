namespace Grid.Terminal;

/// <summary>Interactive authentication and welcome screens; keeps secrets outside command dispatch and history.</summary>
public sealed class TerminalSessionUi(TerminalIdentitySession identity, ITerminalInput input, TextWriter output, Func<bool>? clear = null)
{
    private bool wasAuthenticated;
    public ITerminalInput Input => input;
    public static string Opening(int width = 80) => GridBanner.Render(width);
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
        await TerminalTheme.PromptAsync(output, prompt); await output.FlushAsync(); return await input.ReadAsync(secret, token);
    }
    private async Task<bool> Setup(CancellationToken token)
    {
        if (!input.Interactive) { await output.WriteLineAsync("Error: credential setup requires interactive masked input; redirected setup is refused."); return true; }
        if (!identity.NeedsSetup && !identity.IsMaster) { await output.WriteLineAsync("Access denied. Authenticate the current owner before credential migration."); return true; }
        while (true)
        {
            var saved = await SetupAttempt(token);
            if (saved is null) return false;
            if (saved.Value) { await RefreshAsync(); return true; }
            while (true)
            {
                var retry = await Read("Account was not created. Try setup again? [Y/n] ", false, token);
                if (retry is null) return false;
                retry = retry.Trim();
                if (retry.Length == 0 || retry.Equals("y", StringComparison.OrdinalIgnoreCase)) break;
                if (retry.Equals("n", StringComparison.OrdinalIgnoreCase)) return true;
                await output.WriteLineAsync("Enter y to retry setup or n to remain logged out.");
            }
        }
    }
    private async Task<bool?> SetupAttempt(CancellationToken token)
    {
        await output.WriteLineAsync($"Create local credentials. Existing owner/agent identities and transcripts are preserved. Password: {Grid.Communication.PasswordCredential.MinimumPasswordLength}..{Grid.Communication.PasswordCredential.MaximumPasswordLength} characters. Uppercase, lowercase, numbers and symbols are allowed; no mixture is required.");
        var name = await Read("Username: ", false, token); if (name is null) return null;
        var password = await Read("Password: ", true, token); if (password is null) return null;
        var confirm = await Read("Confirm password: ", true, token); if (confirm is null) return null;
        if (password != confirm)
        {
            await output.WriteLineAsync("Error: passwords do not match. No account was created or changed.");
            password = ""; confirm = "";
            return false;
        }
        var challenges = new List<(string Prompt, string Response, string Confirmation)>();
        await output.WriteLineAsync($"Create three private challenge codes for future identity checks. Prompts and responses must each contain {Grid.Communication.ChallengeCredentialSet.MinimumPromptLength}..{Grid.Communication.ChallengeCredentialSet.MaximumPromptLength} printable characters. Prompts are visible; responses are secret. Prompts and responses must be distinct; responses cannot match the username or password. Avoid public facts, names, and reused passwords.");
        for (var index = 1; index <= Grid.Communication.ChallengeCredentialSet.RequiredCount; index++)
        {
            await output.WriteLineAsync($"Challenge {index} of {Grid.Communication.ChallengeCredentialSet.RequiredCount}");
            var prompt = await Read("Prompt: ", false, token); if (prompt is null) return null;
            var response = await Read("Response: ", true, token); if (response is null) return null;
            var responseConfirmation = await Read("Confirm response: ", true, token); if (responseConfirmation is null) return null;
            challenges.Add((prompt, response, responseConfirmation));
        }
        var saved = false;
        try
        {
            var response = identity.ConfigureCredentials(name, password, confirm, challenges);
            saved = response.StartsWith("Local credentials", StringComparison.Ordinal);
            await output.WriteLineAsync(response);
            if (!saved) await output.WriteLineAsync("No account was created or changed.");
        }
        catch (ArgumentException e) { await output.WriteLineAsync("Error: " + e.Message + " No account was created or changed."); }
        finally
        {
            password = ""; confirm = "";
            for (var index = 0; index < challenges.Count; index++) challenges[index] = (challenges[index].Prompt, "", "");
        } // No secret history/completion/logging; managed strings remain GC-owned.
        return saved;
    }
    public async Task<bool> HandleAsync(CommandResult result, CancellationToken token)
    {
        if (result.RecoveryKeyTarget is { } recoveryTarget) return await HandleRecoveryKey(recoveryTarget, token);
        if (result.ChallengeAction is { } challengeAction) return await HandleChallenge(challengeAction, token);
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
                var welcome = $"Welcome, {TerminalTable.Safe(identity.DisplayName)}\nAuthority level: Master Control\nNo Windows elevation is granted.";
                await GridLoginAnimation.RunAsync(output, welcome,
                    GridLoginAnimation.Enabled(input is ConsoleTerminalInput && input.Interactive,
                        Environment.GetEnvironmentVariable("TERM"), Environment.GetEnvironmentVariable("GRID_ANIMATION")), clear, token);
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
    private async Task<bool> HandleRecoveryKey(string target, CancellationToken token)
    {
        if (!input.Interactive)
        {
            await output.WriteLineAsync("Offline recovery enrollment requires interactive confirmation in the original terminal. Nothing changed.");
            return true;
        }
        await output.WriteLineAsync($"Create a new user-held offline recovery file at:\n{TerminalTable.Safe(target)}\nThe existing recovery file, if any, will become invalid after a successful save. GRID cannot recover a lost key.");
        var answer = await Read("Create and enroll this recovery file? [y/N] ", false, token);
        if (!string.Equals(answer?.Trim(), "y", StringComparison.OrdinalIgnoreCase))
        { await output.WriteLineAsync("Offline recovery enrollment cancelled. Nothing changed."); return true; }
        try { await output.WriteLineAsync(identity.IssueOfflineRecoveryFile(target)); }
        catch (Exception error) when (error is IOException or InvalidDataException or InvalidOperationException or UnauthorizedAccessException or ArgumentException or System.Data.Common.DbException)
        { await output.WriteLineAsync("Offline recovery enrollment failed: " + error.Message + " No recovery file was enrolled."); }
        return true;
    }
    private async Task<bool> HandleChallenge(ChallengeUiAction action, CancellationToken token)
    {
        if (!input.Interactive)
        {
            await output.WriteLineAsync("Challenge management requires interactive masked input; redirected input is refused. Nothing changed.");
            return true;
        }
        if (action == ChallengeUiAction.Disable)
        {
            while (true)
            {
                var answer = await Read("Disable all challenge credentials while retaining password login? [y/N] ", false, token);
                if (answer is null) return false;
                if (string.IsNullOrWhiteSpace(answer) || answer.Trim().Equals("n", StringComparison.OrdinalIgnoreCase))
                { await output.WriteLineAsync("Challenge disable cancelled. Nothing changed."); return true; }
                if (answer.Trim().Equals("y", StringComparison.OrdinalIgnoreCase))
                { await output.WriteLineAsync(identity.DisableChallenges()); return true; }
                await output.WriteLineAsync("Enter y to disable challenges or n to keep them.");
            }
        }
        var prompts = identity.ChallengePrompts();
        if (prompts.Count == 0)
        {
            await output.WriteLineAsync("Challenge credentials are disabled or not enrolled. Use credentials challenges setup.");
            return true;
        }
        var responses = new string[prompts.Count];
        try
        {
            await output.WriteLineAsync("Masked local self-test. All responses are checked together; no per-question result is disclosed or saved.");
            for (var index = 0; index < prompts.Count; index++)
            {
                await output.WriteLineAsync($"Challenge {index + 1} of {prompts.Count}: {TerminalTable.Safe(prompts[index])}");
                var response = await Read("Response: ", true, token);
                if (response is null) return false;
                responses[index] = response;
            }
            await output.WriteLineAsync(identity.TestChallenges(responses));
            return true;
        }
        finally { Array.Fill(responses, ""); }
    }
}
