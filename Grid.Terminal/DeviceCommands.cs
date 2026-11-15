using Grid.Agent;
using Grid.Simulation;
namespace Grid.Terminal;

public static class DeviceCommands
{
    public static void Register(CommandDispatcher commands, GridSimulationEngine engine, TerminalIdentitySession identity, Func<CancellationToken, Task>? save = null)
    {
        commands.Register(new("device", "device <catalog|apps|paths|processes|windows|resources|select|approve|run|revoke> ...",
            "Discover metadata or review one selected device action. Original host only; discovery never grants control.", args =>
        {
            if (!identity.IsMaster || identity.IsAttachment) return new("Device access requires the original Master Control terminal.");
            try
            {
                if (args.Count == 0 || args.SequenceEqual(new[] { "catalog" }))
                    return new(string.Join("\n", DeviceCapabilities.Catalog.Select(c => c.Id + ": " + c.Policy + " | " + c.Limits)));
                if (args.Count == 1 && args[0] == "apps")
                {
                    var apps = engine.GetSnapshot().ApplicationCatalog.Take(256).ToArray();
                    return new("Observed installed/running application catalog; may be stale. No launch grants:\n" +
                        string.Join("\n", apps.Select(a => TerminalTable.Safe(a.Name) + " | last observed " + a.LastSeenUtc.ToString("O"))) + "\nSelect an exact executable path to request launch.");
                }
                if (args.Count == 1 && args[0] == "paths")
                {
                    var tasks = engine.TasksFor(identity.Reference);
                    var paths = tasks.Workspaces.Where(w => w.Enabled && w.Machine == Environment.MachineName).Select(w => "Workspace " + w.Name + ": " + w.Root)
                        .Concat(tasks.Storage.Scopes.Where(s => s.Enabled && s.Machine == Environment.MachineName).Select(s => "Storage " + s.Name + ": " + s.Path));
                    return new("Enabled same-device path registrations; no contents read:\n" + string.Join("\n", paths.Select(TerminalTable.Safe)) + "\nListed scopes retain their existing operation-specific approval requirements.");
                }
                if (args.Count == 1 && args[0] == "resources") return commands.ExecuteTokens(["resources", "details"]);
                if (args.Count == 1 && args[0] is "processes" or "windows")
                {
                    var result = engine.DiscoverDevices(identity.Reference, args[0] == "windows");
                    return new(result.Source + " at " + result.ObservedUtc.ToString("O") + "\n" +
                        TerminalTable.Render(["PID", "Application", "Observation"], result.Items.Select(i => new[] { i.Id, TerminalTable.Safe(i.Name), i.State })) +
                        (result.Incomplete ? "\nIncomplete: a bound or access restriction was reached." : "") + "\nNo window titles or command lines read. Discovery grants no control.");
                }
                if (args.Count == 3 && args[0] == "select")
                {
                    var operation = args[1] switch { "launch" => DeviceOperation.Launch, "focus" => DeviceOperation.Focus,
                        "inspect" => DeviceOperation.InspectWindow, "mkdir" => DeviceOperation.CreateFolder,
                        _ => throw new ArgumentException("Select launch, focus, inspect or mkdir.") };
                    var review = engine.SelectDevice(identity.Reference, new(1, operation, args[2]));
                    return new("REVIEW " + review.Id + "\nAction: " + operation + "\nTarget: " + TerminalTable.Safe(review.Target.Display) +
                        "\nFingerprint: " + review.Target.Fingerprint + "\nExpires: " + review.ExpiresUtc.ToString("O") +
                        "\nNo action executed. Approve this exact selection, then run it.\ndevice approve " + review.Id + "\ndevice run " + review.Id + "\ndevice revoke " + review.Id);
                }
                if (args.Count == 2 && Guid.TryParse(args[1], out var id))
                {
                    switch (args[0])
                    {
                        case "approve": engine.ApproveDevice(identity.Reference, id); return new("Device selection approved for one attempt. Run separately.");
                        case "revoke": engine.RevokeDevice(identity.Reference, id); return new("Device grant revoked.");
                        case "run":
                            if (save is null) return new("Device execution requires saved audit support. Configure a persistent world.");
                            var reference = identity.Reference;
                            return new("Executing the reviewed device action.", BrokerOperation: async token =>
                            {
                                try
                                {
                                    var receipt = await engine.ExecuteDeviceAsync(reference, id, save, token);
                                    return new CommandResult(receipt.Status + ": " + receipt.Detail + "\nReceipt: " + receipt.Id + " at " + receipt.AtUtc.ToString("O"));
                                }
                                catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidOperationException or ArgumentException or OperationCanceledException or System.Data.Common.DbException)
                                { return new CommandResult("Device attempt stopped: " + TerminalTable.Safe(error.Message) + " No automatic retry; select and review again."); }
                            });
                    }
                }
                return new("Use device catalog; device apps/paths/processes/windows/resources; device select launch <quoted-exe-path>, focus/inspect <PID>, or mkdir <quoted-new-folder>; then approve/run/revoke <review-id>.");
            }
            catch (Exception e) when (e is ArgumentException or InvalidOperationException or IOException or UnauthorizedAccessException or NotSupportedException or System.ComponentModel.Win32Exception)
            { return new("Device request unavailable: " + TerminalTable.Safe(e.Message) + " Existing grants are not expanded; no automatic retry."); }
        }) { Capability = "world.admin" });
    }
}
