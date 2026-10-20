namespace Grid.Core;

// Classification is trusted model data, never inferred from a name, job or process.
public enum ResidentKind { Legacy, Application, Agent, Special }
public enum ApplicationActivity { Unknown, Sleeping, OnCall, Running }
public sealed record ProcessLifetime(int Pid, DateTimeOffset StartedAtUtc);
public sealed record ApplicationBinding(string ApplicationKey, Guid Incarnation,
    IReadOnlyList<ProcessLifetime> Processes, DateTimeOffset ObservedAtUtc,
    bool Confirmed, long? WorkingSetBytes, string Source = "Unspecified")
{
    public ApplicationActivity Activity { get; init; } = ApplicationActivity.Running;
}
