namespace Grid.Simulation.Tasks;
public sealed record StorageScope(string Name,string Path,string Machine,bool Enabled);
public sealed record QuotaProposal(Guid Id,Guid TaskId,Guid CoordinatorId,long RequestedBytes,long MeasuredBytes,bool Complete,DateTimeOffset CreatedUtc,Guid RequestedBy,Guid? ApprovedBy,DateTimeOffset? ApprovedUtc);
public sealed record StorageState
{
    public const long InitialQuota=20_000_000_000;
    public long QuotaBytes { get; init; } = InitialQuota;
    public IReadOnlyList<StorageScope> Scopes { get; init; } = Array.Empty<StorageScope>();
    public IReadOnlyList<QuotaProposal> Proposals { get; init; } = Array.Empty<QuotaProposal>();
    public StorageState Copy() => this with { Scopes=Array.AsReadOnly(Scopes.ToArray()),Proposals=Array.AsReadOnly(Proposals.ToArray()) };
    public static void Validate(StorageState state,TaskState tasks)
    {
        if(state is null || state.QuotaBytes < InitialQuota || state.QuotaBytes>1_000_000_000_000 || state.Scopes is null || state.Proposals is null || state.Scopes.Count>32 || state.Proposals.Count>32 ||
           state.Scopes.Any(s=>s is null || !DevelopmentPolicy.Name(s.Name) || !TaskState.Label(s.Path,1024) || !Path.IsPathFullyQualified(s.Path) || !TaskState.Label(s.Machine,256)) || state.Scopes.Select(s=>s.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count()!=state.Scopes.Count)
            throw new InvalidDataException("Invalid JN storage state.");
        if(state.Proposals.Any(p=>p is null) || state.Proposals.Select(p=>p.Id).Distinct().Count()!=state.Proposals.Count || state.Proposals.Select(p=>p.TaskId).Distinct().Count()!=state.Proposals.Count) throw new InvalidDataException("Duplicate quota proposals.");
        foreach(var p in state.Proposals)
            if(p.Id==Guid.Empty || p.RequestedBy==Guid.Empty || p.ApprovedBy==Guid.Empty || p.RequestedBytes<=InitialQuota || p.RequestedBytes>1_000_000_000_000 || p.MeasuredBytes<0 || p.CreatedUtc.Offset!=TimeSpan.Zero ||
                (p.ApprovedBy is null)!=(p.ApprovedUtc is null) || p.ApprovedUtc is {} utc && (utc.Offset!=TimeSpan.Zero || utc<p.CreatedUtc) ||
                !tasks.Agents.Any(a=>a.Id==p.CoordinatorId && a.Role==AgentRole.MCP) || !tasks.Records.Any(t=>t.Id==p.TaskId && t.Capability=="jn.quota" && t.OperatorId==p.RequestedBy && long.TryParse(t.Argument,out var gb) && gb*1_000_000_000==p.RequestedBytes && (p.ApprovedBy is null || t.Status==TaskStatus.Completed && p.ApprovedBy==p.RequestedBy)))
                throw new InvalidDataException("Invalid quota proposal linkage.");
        var expected=state.Proposals.Where(p=>p.ApprovedBy is not null).Select(p=>p.RequestedBytes).DefaultIfEmpty(InitialQuota).Max();
        if(state.QuotaBytes!=expected) throw new InvalidDataException("Quota does not match approved history.");
    }
}
public static class StoragePolicy
{
    public static bool IsReal(string capability) => capability is "jn.inventory" or "jn.duplicates" or "jn.cleanup" or "jn.storage" or "jn.quota";
    public static bool Valid(string capability,string argument) => capability switch
    {
        "jn.inventory" or "jn.duplicates" or "jn.cleanup" => DevelopmentPolicy.Name(argument),
        "jn.storage" => argument=="status",
        "jn.quota" => int.TryParse(argument,out var gb) && gb is >20 and <=1000 && argument.All(char.IsAsciiDigit),
        _ => false
    };
}
