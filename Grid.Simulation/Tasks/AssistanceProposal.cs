using System.Text.Json;
namespace Grid.Simulation.Tasks;

public sealed record AssistanceGuard(Guid ConversationId,long Activity,long Context,long Memory);
public sealed record AssistanceProposal(IReadOnlyList<DelegationStep> Steps)
{
    public int Version => 1;
    public string Request => string.Join("; ",Steps.Select(s => $"{s.Role}: {s.Capability[(s.Capability.IndexOf('.')+1)..]} {s.Argument}"));
    public static AssistanceProposal Parse(string json)
    {
        if (json.Length > 8192) throw new ArgumentException("Proposal is too large.");
        using var doc=JsonDocument.Parse(json,new JsonDocumentOptions { MaxDepth=8 });
        if (doc.RootElement.ValueKind != JsonValueKind.Object) throw new ArgumentException("Expected a proposal object.");
        static void Fields(JsonElement e,params string[] names)
        {
            if(e.ValueKind!=JsonValueKind.Object) throw new ArgumentException("Expected a proposal object.");
            var fields=e.EnumerateObject().Select(p=>p.Name).ToArray();
            if(fields.Length!=names.Length || fields.Distinct().Count()!=fields.Length || fields.Except(names).Any()) throw new ArgumentException("Unexpected or duplicate proposal fields.");
        }
        if (doc.RootElement.TryGetProperty("version", out var version))
        { Fields(doc.RootElement,"version","steps"); if (version.ValueKind != JsonValueKind.Number || !version.TryGetInt32(out var number) || number != 1) throw new ArgumentException("Unsupported task plan version."); }
        else Fields(doc.RootElement,"steps"); // Legacy v1 producers remain compatible.
        var array=doc.RootElement.GetProperty("steps");
        if(array.ValueKind!=JsonValueKind.Array || array.GetArrayLength() is <1 or >8) throw new ArgumentException("Proposal requires 1–8 steps.");
        var steps=new List<DelegationStep>();
        foreach(var item in array.EnumerateArray())
        {
            Fields(item,"agent","capability","argument");
            if (item.EnumerateObject().Any(field => field.Value.ValueKind != JsonValueKind.String))
                throw new ArgumentException("Proposal fields must be strings.");
            var roleText=item.GetProperty("agent").GetString(); var capability=item.GetProperty("capability").GetString(); var argument=item.GetProperty("argument").GetString();
            if(!Enum.TryParse<AgentRole>(roleText,true,out var role) || !Enum.IsDefined(role) || role==AgentRole.MCP || int.TryParse(roleText,out _) ||
                capability is null || argument is null || argument.Contains(';') || !capability.StartsWith(role.ToString().ToLowerInvariant()+".",StringComparison.Ordinal) || !TaskCapabilities.Valid(role,capability,argument))
                throw new ArgumentException("Unsupported agent, capability or argument. No tasks created.");
            steps.Add(new(role,capability,argument));
        }
        var result=new AssistanceProposal(Array.AsReadOnly(steps.ToArray()));
        _=CoordinationRequest.Parse(result.Request);
        return result;
    }
}
