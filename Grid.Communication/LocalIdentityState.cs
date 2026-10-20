namespace Grid.Communication;

public sealed record LocalIdentityState(string? OwnerSubject, IReadOnlyList<Guid> RegisteredAgents)
{
    public static LocalIdentityState Empty => new(null, []);
    public static void Validate(LocalIdentityState state)
    {
        if (state is null || state.RegisteredAgents is null || state.RegisteredAgents.Count > 256 ||
            state.RegisteredAgents.Any(id => id == Guid.Empty) || state.RegisteredAgents.Distinct().Count() != state.RegisteredAgents.Count ||
            state.OwnerSubject is null && state.RegisteredAgents.Count != 0 ||
            state.OwnerSubject is { } owner && (string.IsNullOrWhiteSpace(owner) || owner.Length > 256 || owner.Any(char.IsControl)))
            throw new InvalidDataException("Invalid local identity registry.");
    }
}
