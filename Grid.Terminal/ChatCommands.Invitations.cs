using System.Text.RegularExpressions;
using Grid.Communication;
using Grid.Simulation.Tasks;
namespace Grid.Terminal;

public sealed partial class ChatCommands
{
    private sealed record Invitation(FollowUp Scope, string Agent, bool Remove, int Messages);
    private Invitation? invitation;
    private bool InvitationTalk(string input, out CommandResult reply)
    {
        reply = new("");
        if (input.Equals("participants", StringComparison.OrdinalIgnoreCase)) { reply = Explain("Current participants: " + Contacts(Find(active!.Value))); return true; }
        var confirm = Regex.IsMatch(input, @"^(accept invitation|confirm removal|yes)$", RegexOptions.IgnoreCase);
        var decline = Regex.IsMatch(input, @"^(decline invitation|cancel invitation|no)$", RegexOptions.IgnoreCase);
        if (invitation is not null && !confirm && !decline) invitation = null;
        if (invitation is {} pending && (confirm || decline))
        {
            invitation = null;
            if (decline) { reply = Explain("Invitation/removal declined. Membership unchanged."); return true; }
            if (!Current(pending.Scope)) { reply = Explain("Invitation expired after a scope change. Request it again."); return true; }
            try
            {
                active = engine.ChangeConversationMembership(identity!.Reference, pending.Scope.Conversation, pending.Agent, pending.Remove,
                    pending.Messages, pending.Scope.Project, pending.Scope.Memory, pending.Scope.Activity);
                followUp = null; assistanceDraft = null; assistanceRequest++; CancelFileRequest(); talkDetails = null; StopScreen();
                save?.Invoke(CancellationToken.None).GetAwaiter().GetResult();
                reply = Explain((pending.Remove ? "Removed " : "Invited ") + pending.Agent + ". Current participants: " + Contacts(Find(active.Value)) +
                    "\nContinuing in this window with a fresh shared-history segment. Earlier transcripts are retained, not automatically shared. No file or memory grants changed.\nAddress an agent by name or @NAME. Use mcp plans/brief for recorded progress; task and edit approvals remain separate.");
            }
            catch (Exception e) when (e is ArgumentException or InvalidOperationException or UnauthorizedAccessException) { reply = Explain("Membership unchanged: " + e.Message); }
            catch (Exception e) when (e is IOException or Microsoft.Data.Sqlite.SqliteException) { reply = Explain("Membership save failed; the change may exist in memory. Resolve persistence before continuing."); }
            return true;
        }
        if (input.Equals("accept invitation", StringComparison.OrdinalIgnoreCase) || input.Equals("confirm removal", StringComparison.OrdinalIgnoreCase) || input.Equals("decline invitation", StringComparison.OrdinalIgnoreCase))
        { reply = Explain("No pending invitation/removal. Request a new preview."); return true; }
        var request = Regex.Match(input, @"^(invite|remove participant|remove agent)\s+(.+?)[.!]?$", RegexOptions.IgnoreCase);
        if (!request.Success) return false;
        if (identity?.IsMaster != true) { reply = Explain("Only human Master Control can confirm specialist membership."); return true; }
        var c = Find(active!.Value); var state = engine.TasksFor(identity.Reference);
        var coordinator = state.Agents.SingleOrDefault(a => a.Role == AgentRole.MCP);
        if (coordinator is null || !c.Participants.Any(p => p.Id == coordinator.Id)) { reply = Explain("Connect to MCP first."); return true; }
        var name = request.Groups[2].Value.Trim();
        var remove = !request.Groups[1].Value.Equals("invite", StringComparison.OrdinalIgnoreCase);
        var agent = state.Agents.SingleOrDefault(a => a.Role.ToString().Equals(name, StringComparison.OrdinalIgnoreCase));
        if (agent is null && !remove)
        {
            // Recommendation uses registered tool purposes; it never grants membership by itself.
            var terms = Regex.Matches(name.ToLowerInvariant(), @"[a-z]+").Select(m => m.Value).Where(w => w.Length > 2).ToHashSet();
            string Expertise(AgentRole role) => role switch { AgentRole.MAXIS => "code software debugging builds tests", AgentRole.JN => "files storage duplicates backups", AgentRole.SENTINEL => "security firewall malware", AgentRole.VOLT => "resources cpu gpu ram performance", AgentRole.RELAY => "notifications messages delivery", _ => "" };
            var scored = McpCapabilityCatalog.Entries.GroupBy(e => e.Agent).Select(g => (Role: g.Key, Score: terms.Count(w => Expertise(g.Key).Contains(w, StringComparison.OrdinalIgnoreCase)) * 10 + g.Sum(e => terms.Count(w => (e.Purpose + " " + e.Capability).Contains(w, StringComparison.OrdinalIgnoreCase)))))
                .Where(x => x.Score > 0).OrderByDescending(x => x.Score).ToArray();
            if (scored.Length > 0 && (scored.Length == 1 || scored[0].Score > scored[1].Score)) agent = state.Agents.SingleOrDefault(a => a.Role == scored[0].Role);
        }
        if (agent is null) { reply = Explain("Which specialist? Invite MAXIS, JN, SENTINEL, VOLT or RELAY; for example invite someone for code debugging."); return true; }
        if (agent.Role == AgentRole.MCP || c.Participants.Any(p => p.Id == agent.Id) != remove) { reply = Explain(agent.Role == AgentRole.MCP ? "MCP stays as coordinator." : remove ? "That specialist is not present." : "That specialist is already present."); return true; }
        invitation = new(Pending("invitation", input), agent.Role.ToString(), remove, c.Messages.Count);
        var purpose = string.Join("; ", McpCapabilityCatalog.Entries.Where(e => e.Agent == agent.Role).Take(3).Select(e => e.Purpose));
        reply = Explain((remove ? "Remove " : "MCP suggests inviting ") + agent.Role + "? " + purpose +
            "\nCurrent participants: " + Contacts(c) +
            "\nConfirmation starts a fresh segment in this window and clears unsubmitted drafts. New messages are shared with the resulting group; earlier transcripts, private files and memories are not automatically shared. No task authority is granted.\nReply " + (remove ? "confirm removal" : "accept invitation") + " or decline invitation.");
        return true;
    }
}
