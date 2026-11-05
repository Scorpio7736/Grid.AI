using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Grid.Core.Generation;
using Grid.Simulation;
using Grid.Simulation.Tasks;
using Status=Grid.Simulation.Tasks.TaskStatus;
namespace Grid.Terminal;

public sealed record PortableDeviceEntry(string Fingerprint,string PackageRoot,HardwareProfile Hardware);
public static class PortableDevice
{
    public static string Fingerprint()
    {
        var key=OperatingSystem.IsWindows() ? Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Cryptography","MachineGuid",null) as string : null;
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes((key ?? Environment.MachineName)+"|"+Environment.MachineName)));
    }
    public static IReadOnlyList<PortableDeviceEntry> Read(string root)
    {
        var file=Path.Combine(root,"data","devices.json"); if(!File.Exists(file)) return [];
        if(new FileInfo(file).Length>1024*1024) throw new InvalidDataException("Device registry exceeds limit.");
        var entries=JsonSerializer.Deserialize<PortableDeviceEntry[]>(File.ReadAllText(file)) ?? throw new InvalidDataException("Invalid device registry.");
        if(entries.Length>32 || entries.Any(e=>e is null || e.Fingerprint is null || e.Fingerprint.Length!=64 || string.IsNullOrWhiteSpace(e.PackageRoot) || !Path.IsPathFullyQualified(e.PackageRoot) || e.Hardware is null)) throw new InvalidDataException("Invalid device registry.");
        return entries;
    }
    public static bool Changed(string root,string fingerprint) => !Read(root).Any(d=>d.Fingerprint==fingerprint && d.PackageRoot.Equals(Path.GetFullPath(root),StringComparison.OrdinalIgnoreCase));
    public static void Remember(string root,string fingerprint,HardwareProfile hardware)
    {
        var records=Read(root).Where(d=>d.Fingerprint!=fingerprint).Append(new PortableDeviceEntry(fingerprint,Path.GetFullPath(root),HardwareProfile.Normalize(hardware))).ToArray();
        if(records.Length>32) throw new IOException("Device registry full; use one time.");
        var path=Path.Combine(root,"data","devices.json"); Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path+".tmp",JsonSerializer.Serialize(records));File.Move(path+".tmp",path,true);
    }
    public static GridSimulationSnapshot Reauthorize(GridSimulationSnapshot state)
    {
        var tasks=state.Tasks;
        var records=tasks.Records.Select(t=> t.Status is Status.Queued or Status.Running ? t with {
            Status=t.Status==Status.Running ? Status.Interrupted : Status.AwaitingApproval,
            ApprovedBy=null, Result=t.Status==Status.Running ? "Interrupted by portable device/location change; source approvals do not carry over." : null,
            Audit=Array.AsReadOnly(t.Audit.Append(new TaskAudit(DateTimeOffset.UtcNow,"PortableBoundary",null,null,"Device/location changed; approval invalidated. Review and approve work here.")).ToArray())
        } : t).ToArray();
        var updated=tasks with { Records=Array.AsReadOnly(records), Workspaces=Array.AsReadOnly(tasks.Workspaces.Select(w=>w with { Enabled=false }).ToArray()),
            Storage=tasks.Storage with { Scopes=Array.AsReadOnly(tasks.Storage.Scopes.Select(s=>s with {Enabled=false}).ToArray()) } };
        TaskState.Validate(updated);
        return state with { IsRunning=false,Tasks=updated };
    }
}
