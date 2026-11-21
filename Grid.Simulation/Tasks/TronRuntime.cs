using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
namespace Grid.Simulation.Tasks;

public interface ITronProbe
{
    Task<string> ReadAsync(string capability, CancellationToken token);
}
public static class TronPolicy
{
    public static bool IsReal(string capability) => capability is "tron.defender" or "tron.firewall" or "tron.events";
    public static bool Valid(string capability, string argument) => capability is "tron.defender" or "tron.firewall" ? argument == "status" : capability == "tron.events" && argument == "recent";
    public static string Render(string capability, string json, DateTimeOffset observed)
    {
        if (!IsReal(capability) || json.Length > 32768) throw new InvalidDataException("Invalid TRON observation.");
        using var document = JsonDocument.Parse(json); var root = document.RootElement;
        if (!root.TryGetProperty("available",out var available) || available.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) throw new InvalidDataException("Invalid TRON availability.");
        var source = capability switch { "tron.defender" => "Windows Defender / Get-MpComputerStatus", "tron.firewall" => "Windows Firewall / ActiveStore", _ => "Windows Security log / Get-WinEvent" };
        var fields = new List<string> { "[REAL HOST SECURITY - READ ONLY]", "Observed UTC: " + observed.ToUniversalTime().ToString("O"), "Source: " + source };
        string Value(JsonElement item,string name)
        {
            if (!item.TryGetProperty(name,out var value) || value.ValueKind == JsonValueKind.Null) return "Unknown";
            return new string(value.ToString().Where(c => !char.IsControl(c) && c != '|').Take(100).ToArray());
        }
        string Flag(JsonElement item,string name) => !item.TryGetProperty(name,out var v) ? "Unknown" : v.ValueKind switch { JsonValueKind.True => "Enabled", JsonValueKind.False => "Disabled", _ => "Unknown" };
        string Timestamp(string name)
        {
            var value=Value(root,name);
            return DateTimeOffset.TryParse(value,CultureInfo.InvariantCulture,DateTimeStyles.AssumeUniversal,out var time) && time.Year > 1601 ? time.ToUniversalTime().ToString("O") : "Unknown / not recorded";
        }
        if (!available.GetBoolean())
        {
            fields.Add("UNAVAILABLE: " + Value(root,"reason")); fields.Add("Protection is unknown; no clean/safe conclusion. No elevation requested.");
        }
        else if (capability == "tron.defender")
        {
            foreach(var name in new[]{"AntivirusEnabled","RealTimeProtectionEnabled","AMServiceEnabled"}) fields.Add(name + ": " + Flag(root,name));
            fields.Add("Running mode: " + Value(root,"AMRunningMode"));
            fields.Add("Signature updated UTC: " + Timestamp("SignatureUpdated"));
            fields.Add("Last quick scan ended UTC: " + Timestamp("QuickScanEnd"));
            fields.Add("Last full scan ended UTC: " + Timestamp("FullScanEnd"));
            if(new[]{"AntivirusEnabled","RealTimeProtectionEnabled","AMServiceEnabled"}.Any(n=>Flag(root,n)=="Disabled")) fields.Add("WARNING: Defender reports a disabled protection component; review Windows Security and any third-party antivirus.");
            fields.Add("No scan started. Status is not a malware-free guarantee.");
        }
        else if (capability == "tron.firewall")
        {
            if(!root.TryGetProperty("profiles",out var profiles) || profiles.ValueKind != JsonValueKind.Array || profiles.GetArrayLength()>3) throw new InvalidDataException("Invalid firewall profile observation.");
            foreach(var profile in profiles.EnumerateArray()) fields.Add(Value(profile,"Name") + ": " + Flag(profile,"Enabled") + "; inbound=" + Value(profile,"Inbound") + "; outbound=" + Value(profile,"Outbound"));
            if(profiles.GetArrayLength()!=3) fields.Add("WARNING: incomplete profile coverage; missing profiles are unknown.");
            if(profiles.EnumerateArray().Any(p=>Flag(p,"Enabled")=="Disabled")) fields.Add("WARNING: at least one firewall profile is disabled.");
            fields.Add("Profile configuration only; not a rule audit or proof of effective network isolation.");
        }
        else
        {
            if(!root.TryGetProperty("events",out var events) || events.ValueKind != JsonValueKind.Array || events.GetArrayLength()>10) throw new InvalidDataException("Invalid security event observation.");
            fields.Add("Newest up to 10 events in the last 24 hours; bounded sample, not a threat verdict.");
            foreach(var item in events.EnumerateArray()) fields.Add("Event " + Value(item,"Id") + " record " + Value(item,"RecordId") + " UTC " + Value(item,"TimeUtc"));
            if(events.GetArrayLength()==0) fields.Add("No matching events in this query; this does not establish host safety.");
            fields.Add("Messages, account names, IP addresses and event payloads are not collected.");
        }
        var text=string.Join(" | ",fields);
        if(text.Length>2048) throw new InvalidDataException("TRON summary exceeded its saved result limit.");
        return text;
    }
}

/// <summary>Fixed read-only Windows commands. No user text enters PowerShell.</summary>
public sealed class WindowsTronProbe : ITronProbe
{
    public async Task<string> ReadAsync(string capability,CancellationToken token)
    {
        if(!TronPolicy.IsReal(capability)) throw new ArgumentException("Unsupported TRON action; remediation is disabled.");
        if(!OperatingSystem.IsWindows()) return "{\"available\":false,\"reason\":\"Windows is required\"}";
        var body = capability switch
        {
            "tron.defender" => """
                Import-Module (Join-Path $PSHOME 'Modules/Defender/Defender.psd1') -ErrorAction Stop
                $s = Defender\Get-MpComputerStatus -ErrorAction Stop
                function Stamp($v) { if ($null -ne $v -and $v.Year -gt 1601) { $v.ToUniversalTime().ToString('o') } else { $null } }
                @{available=$true; AntivirusEnabled=$s.AntivirusEnabled; RealTimeProtectionEnabled=$s.RealTimeProtectionEnabled; AMServiceEnabled=$s.AMServiceEnabled; AMRunningMode=$s.AMRunningMode; SignatureUpdated=(Stamp $s.AntivirusSignatureLastUpdated); QuickScanEnd=(Stamp $s.QuickScanEndTime); FullScanEnd=(Stamp $s.FullScanEndTime)} | ConvertTo-Json -Compress
                """,
            "tron.firewall" => """
                Import-Module (Join-Path $PSHOME 'Modules/NetSecurity/NetSecurity.psd1') -ErrorAction Stop
                $p = @(NetSecurity\Get-NetFirewallProfile -PolicyStore ActiveStore -ErrorAction Stop | ForEach-Object { @{Name=$_.Name; Enabled=($(if ([int]$_.Enabled -eq 1) {$true} elseif ([int]$_.Enabled -eq 0) {$false} else {$null})); Inbound=$_.DefaultInboundAction.ToString(); Outbound=$_.DefaultOutboundAction.ToString()} })
                @{available=$true; profiles=$p} | ConvertTo-Json -Compress -Depth 4
                """,
            _ => """
                try { $e = @(Microsoft.PowerShell.Diagnostics\Get-WinEvent -FilterHashtable @{LogName='Security'; StartTime=(Get-Date).AddHours(-24)} -MaxEvents 10 -ErrorAction Stop) }
                catch { if ($_.FullyQualifiedErrorId -like 'NoMatchingEventsFound*') {$e=@()} else {throw} }
                $rows = @($e | ForEach-Object { @{Id=$_.Id; RecordId=$_.RecordId; TimeUtc=$_.TimeCreated.ToUniversalTime().ToString('o')} })
                @{available=$true; events=$rows} | ConvertTo-Json -Compress -Depth 4
                """
        };
        var script = "$ErrorActionPreference='Stop'; $ProgressPreference='SilentlyContinue'; [Console]::OutputEncoding=[Text.UTF8Encoding]::new($false); try { " + body + " } catch { @{available=$false; reason=$_.FullyQualifiedErrorId} | ConvertTo-Json -Compress }";
        var path=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),"WindowsPowerShell","v1.0","powershell.exe");
        if(!File.Exists(path)) return "{\"available\":false,\"reason\":\"Windows PowerShell is unavailable\"}";
        var start=new ProcessStartInfo(path) { UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true,StandardOutputEncoding=Encoding.UTF8,StandardErrorEncoding=Encoding.UTF8 };
        foreach(var argument in new[]{"-NoLogo","-NoProfile","-NonInteractive","-EncodedCommand",Convert.ToBase64String(Encoding.Unicode.GetBytes(script))}) start.ArgumentList.Add(argument);
        // Use the machine module directory, not user-installed/profile modules with matching command names.
        start.Environment["PSModulePath"] = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),"WindowsPowerShell","v1.0","Modules");
        using var deadline=CancellationTokenSource.CreateLinkedTokenSource(token); deadline.CancelAfter(TimeSpan.FromSeconds(30));
        using var process=new Process { StartInfo=start }; token.ThrowIfCancellationRequested(); process.Start();
        async Task<string> Read(StreamReader reader)
        {
            var text=new StringBuilder(); var buffer=new char[1024]; int count;
            while((count=await reader.ReadAsync(buffer.AsMemory(),deadline.Token))>0)
            { if(text.Length+count>32768) { deadline.Cancel(); throw new IOException("Security provider output exceeded capture limit."); } text.Append(buffer,0,count); }
            return text.ToString();
        }
        var stdout=Read(process.StandardOutput); var stderr=Read(process.StandardError);
        try
        {
            await process.WaitForExitAsync(deadline.Token);
            var json=await stdout; await stderr;
            if(process.ExitCode!=0) throw new IOException("Windows security provider exited unsuccessfully; protection state unknown.");
            return json.Trim();
        }
        finally
        {
            deadline.Cancel();
            if(!process.HasExited) { try { process.Kill(entireProcessTree:true); } catch(InvalidOperationException) { } }
            await process.WaitForExitAsync(CancellationToken.None);
            try { await Task.WhenAll(stdout,stderr); } catch(OperationCanceledException) { }
        }
    }
}
