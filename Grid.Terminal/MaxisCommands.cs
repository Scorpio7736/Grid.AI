using Grid.Simulation;
namespace Grid.Terminal;
public static class MaxisCommands
{
    public static void Register(CommandDispatcher commands, GridSimulationEngine engine, TerminalIdentitySession identity, Func<CancellationToken,Task>? save)
    {
        commands.Register(new("maxis", "maxis <workspaces|register|revoke> ...", "Register development workspaces; propose approved work using tasks or mcp.", args =>
        {
            try
            {
                if (args.Count == 0 || args.Count == 1 && args[0] == "workspaces")
                    return new(TerminalTable.Render(["Alias","Root","Project","Enabled"],engine.TasksFor(identity.Reference).Workspaces.Select(w=>new[]{w.Name,w.Root,w.Project,w.Enabled.ToString()})));
                if (save is null) return new("MAXIS requires a saved world.");
                if (args.Count == 4 && args[0] == "register")
                {
                    engine.RegisterDevelopmentWorkspace(identity.Reference,args[1],args[2],args[3]); save(CancellationToken.None).GetAwaiter().GetResult();
                    return new("Workspace registered and saved. Each task requires human approval.\nBuild/test executes project-defined code as your Windows user and may write bin/obj or other project-defined outputs. No OS sandbox or network isolation is provided.\nInspect: tasks propose MAXIS maxis.inspect \"" + args[1] + "|relative/file.cs\"\nBuild: tasks propose MAXIS maxis.build " + args[1]);
                }
                if (args.Count == 2 && args[0] == "revoke")
                {
                    engine.RevokeDevelopmentWorkspace(identity.Reference,args[1]); save(CancellationToken.None).GetAwaiter().GetResult(); return new("Workspace revoked; pending tasks cannot start and running work receives cancellation.");
                }
                return new("Usage:\n  maxis workspaces\n  maxis register <alias> \"absolute-root\" \"relative/project.csproj\"\n  maxis revoke <alias>\nExample: maxis register grid \"C:\\Users\\jack\\source\\repos\\Grid7736\" \"Grid.Core/Grid.Core.csproj\"");
            }
            catch (Exception e) when (e is ArgumentException or InvalidOperationException or IOException or UnauthorizedAccessException or System.Data.Common.DbException)
            { return new("MAXIS error: " + e.Message + (e is IOException or System.Data.Common.DbException ? " In-memory changes may remain if saving failed; resolve storage before continuing." : "")); }
        }) { Capability = "session" });
    }
}
