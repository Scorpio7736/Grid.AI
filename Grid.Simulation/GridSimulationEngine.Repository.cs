using Grid.Simulation.Editing;
using Grid.Simulation.Tasks;

namespace Grid.Simulation;

public sealed partial class GridSimulationEngine
{
    public IReadOnlyList<(Guid Id, string Relative, string State)> FileRecoveryChoices(EditSessionReference session, string alias)
    {
        return WithFileDestination(session, alias, (workspace, artifacts) =>
        {
            var records = new List<(Guid Id, string Relative, string State)>();
            if (!Directory.Exists(artifacts)) return (IReadOnlyList<(Guid, string, string)>)records;
            foreach (var directory in Directory.EnumerateDirectories(artifacts).Take(64))
            {
                if (!Guid.TryParseExact(Path.GetFileName(directory), "N", out var id)) continue;
                try
                {
                    var change = WorkspaceFileChange.Recover(artifacts, workspace.Root, id);
                    records.Add((id, change.Relative, change.RolledBack ? "RolledBack" : "Applied"));
                }
                catch (Exception error) when (error is IOException or ArgumentException or System.Text.Json.JsonException or InvalidOperationException or KeyNotFoundException) { }
            }
            return records.OrderBy(r => r.Relative, StringComparer.OrdinalIgnoreCase).ThenBy(r => r.Id).ToArray();
        });
    }
    public RepositorySearchCapture SearchRepository(EditSessionReference session, string alias, string query)
    {
        using var lease = TaskLease(session, true);
        lock (gate) return RepositoryInspection.Search(DevelopmentTarget("maxis.build", alias), query);
    }
    public RepositoryCapture CaptureRepository(EditSessionReference session, string alias)
    {
        using var lease = TaskLease(session, true);
        lock (gate) return RepositoryInspection.Capture(DevelopmentTarget("maxis.build", alias));
    }

    public IReadOnlyList<RepositoryText> RepositoryInstructions(EditSessionReference session, string alias, string relative)
    {
        using var lease = TaskLease(session, true);
        lock (gate) return RepositoryInspection.Instructions(DevelopmentTarget("maxis.build", alias).Root, relative);
    }

    public string ReadDevelopmentFailure(EditSessionReference session, string alias)
    {
        using var lease = TaskLease(session, true);
        lock (gate)
        {
            _ = DevelopmentTarget("maxis.test", alias);
            var task = tasks.Records.Where(t => t.OperatorId == lease.Identity.OperatorId && !t.Archived &&
                t.Argument.Equals(alias, StringComparison.OrdinalIgnoreCase) && t.Capability is "maxis.build" or "maxis.test" && t.Attempts > 0)
                .OrderBy(t => t.Audit.LastOrDefault()?.AtUtc).LastOrDefault()
                ?? throw new InvalidOperationException("Run and review a build or test for this workspace first.");
            if (task.Status is not (Grid.Simulation.Tasks.TaskStatus.Failed or Grid.Simulation.Tasks.TaskStatus.TimedOut))
                throw new InvalidOperationException("The latest build/test is not a recorded failure. No repair inferred.");
            var output = maxisRuntime!.AttemptDirectory(task with { Attempts = task.Attempts - 1 });
            var logs = new System.Text.StringBuilder($"Recorded task {task.Id}; {task.Capability}; attempt {task.Attempts}; {task.Status}.\n");
            foreach (var name in new[] { "report.txt", "stdout.txt", "stderr.txt" })
            {
                var path = DevelopmentPolicy.CheckedPath(output, name);
                if (!File.Exists(path)) continue;
                using var reader = new StreamReader(path);
                var chars = new char[4000]; var read = reader.ReadBlock(chars, 0, chars.Length);
                logs.Append(name).Append(":\n").Append(chars, 0, read).Append(reader.Peek() >= 0 ? "\n[excerpt truncated]\n" : "\n");
            }
            return logs.ToString();
        }
    }
}
