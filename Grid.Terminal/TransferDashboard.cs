using System.Diagnostics;
using System.Text.Json;
namespace Grid.Terminal;

public sealed record TransferUpdate(string Phase,long BytesDone,long TotalBytes,int FilesDone,int TotalFiles,string Detail,bool Finished=false,bool Failed=false,bool CleanupPending=false,long VerifiedBytes=0);
public sealed class TransferDashboard : IDisposable
{
    private readonly string path; private readonly TransferPlan plan; private readonly DateTimeOffset started=DateTimeOffset.UtcNow;
    private readonly Stopwatch throttle=Stopwatch.StartNew(); private string phase=""; private readonly Queue<string> feed=new();
    private DateTimeOffset? copyStarted;
    internal static double? RemainingCopySeconds(TransferUpdate update,double seconds)
    {
        var work=(double)update.BytesDone+update.VerifiedBytes;
        return update.Phase=="Copying and hashing" && seconds>=2 && work>0 ? Math.Max(0,2d*update.TotalBytes-work)/(work/seconds) : null;
    }
    public bool Opened { get; private set; }
    public static double EstimateSeconds(TransferPlan p) => Math.Max(5,p.Files.Sum(f=>(double)f.Length)/(20*1024*1024)+p.Files.Length*.025);
    public static string Estimate(TransferPlan p) => $"Estimated duration: about {Math.Max(1,Math.Ceiling(EstimateSeconds(p)*.5/60)):N0}–{Math.Max(1,Math.Ceiling(EstimateSeconds(p)*2/60)):N0} minutes (rough estimate; drive speed, small files and verification affect it).";
    public static double Percent(TransferUpdate u) => u.Finished ? (u.Failed ? 0:100) : u.Phase switch {
        "Validating save"=>86,"Validating assets"=>90,"Publishing"=>96,"Cleanup pending"=>98,
        "Copying and hashing"=>Math.Min(85,85*(.75*(u.TotalBytes==0 ? 1:Math.Clamp((double)u.BytesDone/u.TotalBytes,0,1))+.25*(u.TotalFiles==0 ? 1:Math.Clamp((double)u.FilesDone/u.TotalFiles,0,1)))),_=>0 };
    internal TransferDashboard(TransferPlan plan,string path) { this.plan=plan;this.path=path; }
    internal static ProcessStartInfo CreateStartInfo(string script,string statusPath)
    {
        // Suppress the PowerShell console, not its WinForms window. SW_HIDE hides the first GUI form too.
        var start=new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),@"WindowsPowerShell\v1.0\powershell.exe")) { UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Normal };
        foreach(var arg in new[]{"-NoProfile","-STA","-ExecutionPolicy","Bypass","-File",script,"-StatusPath",statusPath}) start.ArgumentList.Add(arg);
        return start;
    }
    public static TransferDashboard? Open(TransferPlan plan)
    {
        if(!OperatingSystem.IsWindows() || Console.IsInputRedirected) return null;
        var folder=Path.Combine(Path.GetTempPath(),"GRID-transfer-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
        var view=new TransferDashboard(plan,Path.Combine(folder,"status.json"));view.Report(new("Preparing",0,plan.Files.Sum(f=>f.Length),0,plan.Files.Length,"Starting verified transfer."));
        try {
            var script=Path.Combine(AppContext.BaseDirectory,"Grid-TransferProgress.ps1");
            var start=CreateStartInfo(script,view.path);
            using var process=Process.Start(start) ?? throw new IOException("Progress window did not start.");
            var deadline=Stopwatch.StartNew();
            while(!File.Exists(view.path+".ready") && !process.HasExited && deadline.ElapsedMilliseconds<5000) Thread.Sleep(50);
            if(!File.Exists(view.path+".ready")) { if(!process.HasExited) process.Kill(entireProcessTree:true);return null; }
            view.Opened=true;return view;
        } catch(Exception e) when(e is IOException or System.ComponentModel.Win32Exception or UnauthorizedAccessException) { return null; }
    }
    public void Report(TransferUpdate update)
    {
        if(update.Phase=="Copying and hashing") copyStarted ??= DateTimeOffset.UtcNow;
        if(phase==update.Phase && !update.Finished && !update.CleanupPending && throttle.ElapsedMilliseconds<250) return;
        phase=update.Phase;throttle.Restart();
        var detail=TerminalTable.Safe(update.Detail);feed.Enqueue($"{DateTime.Now:HH:mm:ss}  {update.Phase}: {detail}");while(feed.Count>7)feed.Dequeue();
        try {
            var now=DateTimeOffset.UtcNow;
            var data=new { StartedUtc=started,UpdatedUtc=now,RemainingCopySeconds=RemainingCopySeconds(update,copyStarted is {} start ? (now-start).TotalSeconds : 0),ParentId=Environment.ProcessId,ParentStarted=Process.GetCurrentProcess().StartTime.ToUniversalTime(),Destination=plan.Destination,Move=plan.Move,InitialSeconds=EstimateSeconds(plan),Percent=Percent(update),Update=update,Feed=feed.ToArray() };
            File.WriteAllText(path+".tmp",JsonSerializer.Serialize(data));File.Move(path+".tmp",path,true);
        } catch(Exception e) when(e is IOException or UnauthorizedAccessException) { }
    }
    public void Dispose() { /* Viewer owns its lifetime; it never owns or cancels the transfer. */ }
}
