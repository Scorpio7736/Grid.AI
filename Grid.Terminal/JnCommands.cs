using Grid.Simulation;
namespace Grid.Terminal;
public static class JnCommands
{
    public static void Register(CommandDispatcher commands,GridSimulationEngine engine,TerminalIdentitySession identity,Func<CancellationToken,Task>? save)
    {
        commands.Register(new("jn","jn <scopes|register|revoke|quota> ...","Register read-only storage scopes; review MCP quota escalations. No deletion capability.",args=>
        {
            try
            {
                if(args.Count==0 || args.Count==1 && args[0]=="scopes")
                    return new(TerminalTable.Render(["Alias","Path","Enabled"],engine.StorageFor(identity.Reference).Scopes.Select(s=>new[]{s.Name,s.Path,s.Enabled.ToString()})));
                if(args.Count==1 && args[0]=="quota")
                {
                    var state=engine.StorageFor(identity.Reference);
                    return new($"LOGICAL GRID QUOTA: {state.QuotaBytes} bytes ({state.QuotaBytes/1_000_000_000} decimal GB)\nTracking budget only; no disk reservation or hard write cap.\n\nMCP QUOTA ESCALATIONS\n"+
                        TerminalTable.Render(["Proposal","GB","Measured bytes","Coverage","State"],state.Proposals.Select(p=>new[]{TaskDisplay.Ref(p.Id,state.Proposals.Select(x=>x.Id)),(p.RequestedBytes/1_000_000_000).ToString(),p.MeasuredBytes.ToString(),p.Complete ? "Sampled roots" : "Partial",p.ApprovedBy is null ? "Needs human approval" : "Approved"}))+"\n\nReview: jn quota inspect <proposal-reference>\nFresh measurement: tasks propose JN jn.storage status");
                }
                if(args.Count==3 && args[0]=="quota" && args[1]=="inspect")
                {
                    var state=engine.StorageFor(identity.Reference); var id=TaskDisplay.Resolve(args[2],state.Proposals.Select(p=>p.Id),"quota proposal"); var p=state.Proposals.Single(p=>p.Id==id);
                    return new($"QUOTA PROPOSAL {p.Id}\nRequested: {p.RequestedBytes} bytes\nCurrent budget: {state.QuotaBytes} bytes\nMeasured: {p.MeasuredBytes} bytes | complete: {p.Complete}\nObserved UTC: {p.CreatedUtc:O}\nCoordinator MCP: {p.CoordinatorId}\nTask: {p.TaskId}\nRequested by: {p.RequestedBy}\nApproved by: {p.ApprovedBy}\nApproval UTC: {p.ApprovedUtc:O}\nReview task evidence before approving. No deletion or disk allocation.\njn quota approve {TaskDisplay.Ref(p.Id,state.Proposals.Select(x=>x.Id))}");
                }
                if(save is null) return new("JN requires a saved world.");
                if(args.Count==3 && args[0]=="register") engine.RegisterStorageScope(identity.Reference,args[1],args[2]);
                else if(args.Count==2 && args[0]=="revoke") engine.RevokeStorageScope(identity.Reference,args[1]);
                else if(args.Count==3 && args[0]=="quota" && args[1]=="approve")
                {
                    var state=engine.StorageFor(identity.Reference); engine.ApproveStorageQuota(identity.Reference,TaskDisplay.Resolve(args[2],state.Proposals.Select(p=>p.Id),"quota proposal"));
                }
                else return new("Usage:\n  jn scopes\n  jn register <alias> \"absolute-file-or-directory\"\n  jn revoke <alias>\n  jn quota\n  jn quota inspect <proposal-reference>\n  jn quota approve <proposal-reference>\nExample: mcp request \"JN: inventory docs\"");
                save(CancellationToken.None).GetAwaiter().GetResult(); return new("JN governance change saved. Use jn scopes or jn quota to inspect. No files deleted.");
            }
            catch(Exception e) when(e is ArgumentException or InvalidOperationException or IOException or InvalidDataException or UnauthorizedAccessException or System.Data.Common.DbException)
            { return new("JN error: "+e.Message+(e is IOException or System.Data.Common.DbException ? " If saving failed, in-memory changes remain; resolve storage before continuing." : "")); }
        }) { Capability="session" });
    }
}
