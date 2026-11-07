using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Grid.Agent;
using Grid.Simulation.Editing;
using Grid.Simulation.Tasks;

namespace Grid.Simulation;

public sealed partial class GridSimulationEngine
{
    public TaskRecord ProposeTool(EditSessionReference session, TaskToolRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return ProposeTask(session, request.AgentId, request.Capability, request.Argument, request.RequestKey);
    }

    public FileReadReceipt ReadFileTool(EditSessionReference session, FileToolRequest request)
    {
        if (request.Operation != FileToolOperation.Read || request.Content is not null)
            throw new ArgumentException("A file read requires a read request.");
        return WithFileDestination(session, request.Scope, (workspace, _) =>
        {
            var path = WorkspaceFileChange.Target(workspace.Root, request.RelativePath);
            var bytes = WorkspaceFileChange.Read(path) ?? throw new IOException("Read target does not exist.");
            return new FileReadReceipt(path, Convert.ToHexString(SHA256.HashData(bytes)), WorkspaceFileChange.Decode(bytes));
        });
    }

    public WorkspaceFileChange StageFileTool(EditSessionReference session, FileToolRequest request)
    {
        var operation = request.Operation switch
        {
            FileToolOperation.Create => WorkspaceFileOperation.Create,
            FileToolOperation.Update => WorkspaceFileOperation.Update,
            FileToolOperation.Delete => WorkspaceFileOperation.Delete,
            _ => throw new ArgumentException("Choose create, update or delete for a staged file change.")
        };
        if (operation == WorkspaceFileOperation.Delete && request.Content is not null)
            throw new ArgumentException("Delete requests cannot include replacement content.");
        return WithFileDestination(session, request.Scope, (workspace, _) =>
            WorkspaceFileChange.StageOperation(workspace.Root, request.RelativePath, operation, request.Content));
    }

    public IReadOnlyList<ToolExecutionReceipt> ToolReceipts(EditSessionReference session, Guid planId)
    {
        var state = TasksFor(session);
        var plan = state.Plans.SingleOrDefault(p => p.Id == planId) ?? throw new ArgumentException("Unknown or inaccessible plan.");
        return plan.TaskIds.Select(id =>
        {
            var task = state.Records.Single(t => t.Id == id);
            var request = JsonSerializer.Serialize(new { task.AgentId, task.Capability, task.Argument, task.OperatorId, task.RequestKey });
            var start = task.Audit.LastOrDefault(a => a.Action == "Started")?.AtUtc;
            var finish = task.Audit.LastOrDefault(a => a.Action is "Finished" or "Cancelled" or "Recovered")?.AtUtc;
            return new ToolExecutionReceipt(task.Id, plan.Id, task.AgentId, task.Capability, task.Status.ToString(), task.Attempts,
                Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request))),
                task.Result is null ? null : Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(task.Result))), start, finish);
        }).ToArray();
    }
}
