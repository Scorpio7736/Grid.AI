using System.Text.RegularExpressions;
using Grid.Simulation.Tasks;
namespace Grid.Terminal;

public sealed partial class ChatCommands
{
    private sealed record FileConversation(FollowUp Scope, string Operation, string Workspace = "", string Target = "", string? Content = null,
        string Step = "workspace", WorkspaceChangeReceipt? Change = null, bool Generate = false, IReadOnlyList<string>? FolderChoices = null);
    private FileConversation? fileRequest;
    private (FollowUp Scope, WorkspaceChangeReceipt Receipt)? lastFileReceipt;
    private CommandResult FileLocation()
    {
        if (lastFileReceipt is not { } last || !Current(last.Scope))
            return Explain("No verified file-operation receipt is available in this conversation scope. I cannot confirm a save location. Use changes list/preview or read the file in its registered workspace.");
        var receipt = Dispatch("changes", "preview", last.Receipt.Id.ToString()).FileChange ?? last.Receipt;
        return Explain("Most recent file-operation receipt (not a fresh filesystem check):\n" +
            "Operation: " + receipt.Operation + " | State: " + receipt.State +
            "\nAbsolute path: " + Grid.Simulation.Tasks.WorkspaceFileChange.Display(receipt.AbsolutePath) +
            "\nChange: " + receipt.Id +
            (receipt.State is "Draft" or "Approved" ? "\nNot applied; this request has not saved a file." :
             receipt.State == "RolledBack" ? "\nThis operation was rolled back." : "\nThis is the recorded operation target; later filesystem changes have not been checked."));
    }
    private static string Unquote(string value)
    {
        var text = value.Trim();
        return text.Length >= 2 && text[0] == text[^1] && text[0] is '\'' or '"' ? text[1..^1] : text;
    }
    private static string NormalizeDestination(string value)
    {
        var name = Unquote(value.Trim());
        name = Environment.ExpandEnvironmentVariables(name);
        if (name.StartsWith("~/") || name.StartsWith("~\\")) name = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), name[2..]);
        if (Path.IsPathFullyQualified(name)) return name;
        name = Regex.Replace(name, @"^(?:my|the)\s+|\s+(?:folder|directory)$", "", RegexOptions.IgnoreCase);
        var simple = Regex.Replace(name, @"^(?:my|the)\s+", "", RegexOptions.IgnoreCase).ToLowerInvariant();
        return simple is "desktop" or "documents" or "downloads" or "pictures" or "music" or "videos" ? "@" + simple : name;
    }
    private static string RandomContent(string request)
    {
        var match = Regex.Match(request.Trim(), @"^(\d{1,3}|one|two|three|four|five|six|seven|eight|nine|ten) random (numbers|letters|words)[.!]?$", RegexOptions.IgnoreCase);
        if (!match.Success) return " with instructions " + request;
        var counts = new[] { "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten" };
        var count = int.TryParse(match.Groups[1].Value, out var n) ? n : Array.IndexOf(counts, match.Groups[1].Value.ToLowerInvariant());
        if (count is < 1 or > 100) return " with instructions " + request;
        var words = new[] { "river", "stone", "cloud", "forest", "light", "meadow", "orbit", "silver", "ocean", "maple", "window", "comet", "garden", "bridge", "sunrise", "willow", "copper", "valley", "feather", "harbor" };
        var kind = match.Groups[2].Value.ToLowerInvariant();
        return " with content " + string.Join("\n", Enumerable.Range(0, count).Select(_ => kind switch
        { "letters" => ((char)('a' + Random.Shared.Next(26))).ToString(), "words" => words[Random.Shared.Next(words.Length)], _ => Random.Shared.Next(1000).ToString() }));
    }
    private void CancelFileRequest()
    {
        if (fileRequest?.Change is { State: "Draft" or "Approved" } change) Dispatch("changes", "discard", change.Id.ToString());
        fileRequest = null;
    }
    private bool TryFileConversation(string input, out CommandResult result)
    {
        result = new("");
        var text = Regex.Replace(input, @"^(?:(?:can|could|would) you\s+|please\s+|I (?:want|need) you to\s+)+", "", RegexOptions.IgnoreCase).Trim();
        var described = Regex.Match(text, @"^(create|write|update|replace)\s+(?:me\s+)?(?:an?\s+)?(?:file\s+)?(.+?)\s+(?:in|on|at)\s+(.+?)\s+(?:that|which)\s+(.+)$", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (described.Success && LooksLikePath(Unquote(described.Groups[2].Value)))
            text = described.Groups[1].Value + " file \"" + Unquote(described.Groups[2].Value) + "\" in \"" +
                NormalizeDestination(described.Groups[3].Value) + "\" with instructions Create the complete file requested: " + described.Groups[4].Value;
        var named = Regex.Match(text, @"^(create|write|update|replace|read|delete|remove) (?:a |the )?file (?:on|in|at|to) (.+?)(?: (?:called|named) (.+?))?(?: and fill it with (.+))?[.!]?$", RegexOptions.IgnoreCase);
        if (named.Success)
        {
            var destination = NormalizeDestination(named.Groups[2].Value);
            text = named.Groups[1].Value + " file \"" + Unquote(named.Groups[3].Value) + "\" in \"" + destination + "\"";
            if (named.Groups[4].Success) text += RandomContent(named.Groups[4].Value);
        }
        var start = Regex.Match(text, @"^(create|write|update|replace|delete|remove|read)\s+(?:(?:a|the)\s+)?(?:file\s*)?(.*)$", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        var recovery = Regex.Match(text, @"^recover change ([a-f0-9-]{36}) in (?:workspace )?(.+)$", RegexOptions.IgnoreCase);
        // Generic analysis and arbitrary deletion requests keep their existing clarification paths.
        var candidate = Regex.Split(start.Groups[2].Value, @"\s+(?:in (?:workspace )?|with (?:content|text|instructions) |containing |to contain |saying )", RegexOptions.IgnoreCase)[0];
        var startsFile = start.Success && (Starts(text, @"^(create|write|update|replace|delete|remove|read) (?:a |the )?file\b") ||
            LooksLikePath(candidate));
        if (fileRequest is { } pending)
        {
            if (!Current(pending.Scope)) { CancelFileRequest(); result = Explain("File request expired because the session or scope changed. Please repeat it."); return true; }
            var explicitChange = Regex.Match(text, @"^(review|approve|apply|rollback|discard) edit ([a-f0-9-]{36})( high-risk)?$", RegexOptions.IgnoreCase);
            if (explicitChange.Success && pending.Change is { } selected && Guid.TryParse(explicitChange.Groups[2].Value, out var selectedId) && selectedId == selected.Id)
            {
                if (explicitChange.Groups[1].Value.Equals("discard", StringComparison.OrdinalIgnoreCase))
                { CancelFileRequest(); result = Explain("Pending draft discarded. Applied changes are not undone."); return true; }
                return ContinueFile(pending, explicitChange.Groups[1].Value + explicitChange.Groups[3].Value, out result);
            }
            if (Starts(text, @"^(go back|back|change target|choose another file)[.!]?$"))
            {
                if (pending.Change is { State: "Applied" or "RolledBack" }) { result = Explain("The change already completed. Ask for another operation, or undo this change."); return true; }
                CancelFileRequest();
                result = AdvanceFile(pending with { Target = "", Content = null, Step = "target", Change = null }); return true;
            }
            if (!startsFile && !recovery.Success && ContinueFile(pending, input, out result)) return true;
            CancelFileRequest(); // Replacement cannot leave a silently approved draft behind.
        }
        if (!startsFile && !recovery.Success) return false;
        followUp = null;
        if (identity?.IsMaster != true) { result = Explain("Access denied. Human Master Control is required for file operations."); return true; }
        if (Find(active!.Value).IsMeeting) { result = Explain("Use a one-to-one conversation for scoped file operations."); return true; }
        if (!dispatcher.IsAllowed("changes")) { result = Explain("File operations are unavailable in this host. Use the original terminal."); return true; }
        if (recovery.Success)
        {
            result = Dispatch("changes", "recover", NormalizeDestination(recovery.Groups[2].Value), recovery.Groups[1].Value);
            if (result.FileChange is { } receipt)
            {
                lastFileReceipt = (Pending("file-operation", input), receipt);
                fileRequest = new(Pending("file-operation", input), "recover", NormalizeDestination(recovery.Groups[2].Value), Step: "done", Change: receipt);
                result = result with { Text = result.Text + "\nSay undo this change to restore the verified backup; newer files will not be overwritten." };
            }
            return true;
        }
        var operation = start.Groups[1].Value.ToLowerInvariant() switch { "write" => "create", "replace" => "update", "remove" => "delete", var op => op };
        var remainder = start.Groups[2].Value.Trim();
        string? content = null; var generate = false;
        var contentMarker = Regex.Match(remainder, @"\s+(?:with (?:content|text|instructions)|containing|to contain|saying)\s+", RegexOptions.IgnoreCase);
        if (contentMarker.Success)
        { generate = contentMarker.Value.Contains("instructions", StringComparison.OrdinalIgnoreCase); content = Unquote(remainder[(contentMarker.Index + contentMarker.Length)..]); remainder = remainder[..contentMarker.Index]; }
        var workspaceMatch = Regex.Match(remainder, "^(?<target>\"[^\"]*\"|'[^']*'|[^\"']*?)\\s+(?:in|on|at|to) (?:workspace )?(?<destination>.+)$", RegexOptions.IgnoreCase);
        var workspace = workspaceMatch.Success ? NormalizeDestination(workspaceMatch.Groups["destination"].Value) : "";
        var target = Unquote(workspaceMatch.Success ? workspaceMatch.Groups["target"].Value : remainder);
        if (workspace.Length == 0 && Path.IsPathFullyQualified(target))
        { workspace = Path.GetDirectoryName(target)!; target = Path.GetFileName(target); }
        if (target is "a file" or "file" or "") target = "";
        if (Regex.IsMatch(target, @"^(all|every|these|those)\b|[?*]", RegexOptions.IgnoreCase))
        { result = Explain("Choose exactly one file and workspace. Bulk operations are not inferred; nothing changed."); return true; }
        result = AdvanceFile(new(Pending("file-operation", input), operation, workspace, target, content, Generate: generate));
        return true;
    }
    private bool ContinueFile(FileConversation p, string input, out CommandResult result)
    {
        result = new(""); var text = input.Trim();
        if (Starts(text, @"^(aplly|aply|appply)\b"))
        {
            result = Explain("Did you mean apply this change? Your pending file request is retained. Nothing was applied. Use apply this change after approval.");
            return true;
        }
        if (p.Step is not ("review" or "approved" or "done") &&
            Starts(text, @"^(approve(?: (?:this|the) change)?(?: high-risk)?|apply(?: (?:this|the) change)?|undo(?: this change)?|rollback|roll back this change|review|show (?:the )?preview)[.!]?$"))
        {
            var next = p.Step switch
            {
                "workspace" when p.FolderChoices is { Count: > 0 } choices =>
                    "First choose a destination by number (for example, 1), or provide its absolute path:\n" +
                    string.Join("\n", choices.Select((path, i) => (i + 1) + ". " + WorkspaceFileChange.Display(path))),
                "workspace" => "First provide the destination folder name, workspace alias, or absolute folder path.",
                "target" => "First provide the filename, for example gridTest.txt.",
                _ => "First provide the file contents, for example content: Hello GRID. For generated content, use draft: followed by your instructions."
            };
            result = Explain("There is no file preview to approve or apply yet. " + next +
                "\nYour request is retained. Nothing was approved or written.");
            return true;
        }
        if (p.Step is "review" or "approved" && Starts(text, @"^(?:draft:|(?:make|expand|revise|rewrite|improve|add|include|change)\b)"))
        {
            var retained = p with { Step = "review", Change = p.Change! with { State = "Draft" } };
            fileRequest = retained;
            var operation = Dispatch("changes", "redraft", p.Change!.Id.ToString(), text);
            result = After(operation, revised =>
            {
                if (!Current(p.Scope) || !ReferenceEquals(fileRequest, retained))
                {
                    if (revised.FileChange is { } abandoned) Dispatch("changes", "discard", abandoned.Id.ToString());
                    return Explain("Revision cancelled or replaced. Nothing applied.");
                }
                if (revised.FileChange is not { } receipt)
                    return revised with { Text = revised.Text + "\nPrevious draft retained; approval cleared. Say review to inspect it or request another revision." };
                fileRequest = retained with { Change = receipt };
                lastFileReceipt = (p.Scope, receipt);
                return revised with { Text = revised.Text + "\nRevised draft; review it and approve again before applying." };
            });
            return true;
        }
        if (p.Step is "review" or "approved" or "done")
        {
            var action = Starts(text, @"^(approve|approve this change|approve the change)( high-risk)?[.!]?$" ) ? "approve" :
                Starts(text, @"^(apply|apply this change|apply the change)[.!]?$" ) ? "apply" :
                Starts(text, @"^(undo|undo this change|rollback|roll back this change)[.!]?$" ) ? "rollback" :
                Starts(text, @"^(review|show (?:the )?preview)[.!]?$" ) ? "preview" : "";
            if (action.Length == 0)
            {
                if (Starts(text, @"^(approve|apply|review|undo|rollback)\b"))
                { result = Explain("Your pending file change is retained. Use review, approve this change (or approve high-risk), apply this change, or undo this change. Nothing executed."); return true; }
                if (p.Step is "review" or "approved")
                {
                    if (text.EndsWith('?') && Starts(text, @"^(?:how|why|who|what (?:is|are))\b"))
                        return false;
                    result = InterpretReviewedFileReply(p, input);
                    return true;
                }
                return false;
            }
            var tokens = new List<string> { "changes", action, p.Change!.Id.ToString() };
            if (action == "approve" && p.Change.HighRisk)
            {
                if (!text.Contains("high-risk", StringComparison.OrdinalIgnoreCase))
                { result = Explain("This exact change deletes a file or alters code/configuration. Review the preview, then say approve high-risk if you accept it. Nothing changed."); return true; }
                tokens.Add("high-risk");
            }
            result = Dispatch(tokens.ToArray());
            if (result.FileChange is { } receipt)
            {
                lastFileReceipt = (p.Scope, receipt);
                fileRequest = p with { Change = receipt, Step = receipt.State == "Approved" ? "approved" : receipt.State == "Draft" ? "review" : "done" };
                result = result with { Text = result.Text + (receipt.State == "Approved" ? "\nSay apply this change to perform this exact approved operation." : receipt.State == "Applied" ? "\nSay undo this change to restore it. Keep the change ID for recovery after restart." : "") };
            }
            return true;
        }
        if (p.Step == "workspace")
        {
            var name = NormalizeDestination(text);
            if (int.TryParse(name, out var choice) && p.FolderChoices is { } choices)
            {
                if (choice < 1 || choice > choices.Count) { result = Explain("Choose one of the listed numbers, or give an absolute folder path."); return true; }
                name = choices[choice - 1];
            }
            if (text.EndsWith('?') || Starts(text, @"^(how|why|what|who|explain)\b")) return false;
            result = AdvanceFile(p with { Workspace = name, FolderChoices = null }); return true;
        }
        if (p.Step == "target")
        {
            if (!LooksLikePath(text)) return false;
            result = AdvanceFile(p with { Target = Unquote(text) }); return true;
        }
        if (p.Step == "content")
        {
            if (Starts(text, @"^(how|why|what|who|can|could|would|explain)\b") || text.EndsWith('?')) return false;
            var explicitDraft = text.StartsWith("draft:", StringComparison.OrdinalIgnoreCase);
            var explicitContent = text.StartsWith("content:", StringComparison.OrdinalIgnoreCase);
            var generate = explicitDraft || !explicitContent && Starts(text, @"^(?:(?:this|it|the file|the page)\s+(?:should|must|needs to)\b|(?:generate|write|make|build)\s+(?:me\s+)?(?:a |an |the )?(?:html|page|website|document|code)\b)");
            var content = explicitDraft ? text[6..].TrimStart() : text.StartsWith("content:", StringComparison.OrdinalIgnoreCase) ? text[8..].TrimStart() : text;
            result = AdvanceFile(p with { Content = content, Generate = generate }); return true;
        }
        return false;
    }
    private CommandResult AdvanceFile(FileConversation request)
    {
        var p = request;
        var workspaces = engine.TasksFor(identity!.Reference).Workspaces.Where(w => w.Enabled).ToArray();
        if (p.Workspace.Length == 0 && workspaces.Length == 1) p = p with { Workspace = workspaces[0].Name };
        if (!Grid.Simulation.GridSimulationEngine.IsFileDestination(p.Workspace) && !workspaces.Any(w => w.Name.Equals(p.Workspace, StringComparison.OrdinalIgnoreCase)))
        {
            var found = engine.FindFileFolders(identity.Reference, p.Workspace);
            if (found.Paths.Count == 1 && !found.Incomplete)
                return AdvanceFile(p with { Workspace = found.Paths[0], FolderChoices = null });
            if (found.Paths.Count > 0)
            {
                fileRequest = p with { Step = "workspace", FolderChoices = found.Paths };
                return Explain("Choose a destination folder by number, or provide its absolute path:\n" +
                    string.Join("\n", found.Paths.Select((path, i) => (i + 1) + ". " + WorkspaceFileChange.Display(path))) +
                    (found.Incomplete ? "\nLookup limit reached; this list may be incomplete." : "") + "\nReply with a folder number first. Approval comes after the file preview. Nothing changed. Your filename and content request are retained.");
            }
            if (workspaces.Length == 0) { fileRequest = p with { Step = "workspace" }; return Explain("Name a destination such as my documents, or provide an existing absolute folder path. No files accessed."); }
            fileRequest = p with { Step = "workspace" };
            var choices = string.Join(", ", workspaces.Select(w => w.Name));
            return Explain((p.Workspace.Length == 0 ? "Which registered workspace should I use? " : "Workspace '" + p.Workspace + "' is not registered. ") +
                "Available: " + choices + ". Reply with an alias, for example " + workspaces[0].Name + ". No folder match was found in the bounded lookup of your home folders and registered workspaces. Give an existing absolute folder path, another folder name, or my documents. Your file request is retained; nothing changed.");
        }
        if (p.Target.Length == 0)
        { fileRequest = p with { Step = "target" }; return Explain($"Which file in {p.Workspace}? Give one relative path, for example notes.md. Paths may contain spaces."); }
        try
        {
            p = p with { Target = engine.WithFileDestination(identity.Reference, p.Workspace, (w, _) =>
            {
                var relative = Path.IsPathFullyQualified(p.Target) ? Path.GetRelativePath(w.Root, p.Target) : p.Target;
                _ = WorkspaceFileChange.Target(w.Root, relative); return relative;
            }) };
        }
        catch (Exception e) when (e is IOException or ArgumentException or InvalidOperationException or UnauthorizedAccessException)
        { fileRequest = p with { Target = "", Step = "target" }; return Explain("File target refused: " + e.Message + " Choose another file; nothing changed."); }
        if (p.Operation is "create" or "update" && p.Content is null)
        {
            fileRequest = p with { Step = "content" };
            return Explain($"What complete text should {p.Workspace}/{p.Target} contain? Give the complete text, or say draft: followed by what you want written. I will show a full preview before approval.");
        }
        var tokens = new List<string> { "changes", p.Generate ? "draft-" + p.Operation : p.Operation, p.Workspace, p.Target };
        if (p.Operation is "create" or "update") tokens.Add(p.Content!);
        fileRequest = p;
        var operation = Dispatch(tokens.ToArray());
        if (operation.BrokerOperation is { } generate)
            operation = operation with { BrokerOperation = token => Current(p.Scope) && ReferenceEquals(fileRequest, p)
                ? generate(token) : Task.FromResult(Explain("File request cancelled or replaced.")) };
        return After(operation, result =>
        {
            if (!Current(p.Scope) || !ReferenceEquals(fileRequest, p))
            {
                if (result.FileChange is { } cancelled) Dispatch("changes", "discard", cancelled.Id.ToString());
                return Explain("File request cancelled or replaced. Nothing applied.");
            }
            if (result.FileChange is { } receipt)
            {
                lastFileReceipt = (p.Scope, receipt);
                fileRequest = p with { Change = receipt, Step = "review" };
                return result with { Text = result.Text + (receipt.HighRisk ? "\nTo approve this exact change, say approve high-risk. Then say apply this change." : "\nTo approve this exact change, say approve this change. Then say apply this change.") };
            }
            fileRequest = null;
            return result;
        });
    }
}
