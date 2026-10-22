using System.Security.Cryptography;
using System.Text.Json;
namespace Grid.Simulation.Tasks;
public sealed record StorageFile(string Path,long Bytes,DateTime LastWriteUtc,string? Sha256=null);
public sealed record JnReport(string Summary,long Bytes,bool Complete);
public sealed class JnRuntime(string databasePath,string contentRoot)
{
    private readonly string database=Path.GetFullPath(databasePath);
    private readonly string content=Path.GetFullPath(contentRoot);
    private string Artifacts => database+".jn-artifacts";
    public void CheckScope(StorageScope scope)
    {
        if(!scope.Enabled || scope.Machine!=Environment.MachineName) throw new InvalidOperationException("Storage scope revoked or registered on another machine.");
        var path=DevelopmentPolicy.CheckedPath(scope.Path);
        if(!File.Exists(path) && !Directory.Exists(path)) throw new IOException("Registered storage path is missing.");
        bool Within(string a,string b) => a.Equals(b,StringComparison.OrdinalIgnoreCase) || a.StartsWith(b.TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase);
        if(Within(path,Artifacts) || Within(Artifacts,path)) throw new ArgumentException("Registered inventory scope must not overlap JN report storage.");
    }
    public Task<JnReport> ExecuteAsync(TaskRecord task,StorageScope? scope,long quota,CancellationToken token) => Task.Run(async () =>
    {
        if(scope is not null) CheckScope(scope);
        var managed=task.Capability is "jn.storage" or "jn.quota";
        var roots=managed ? new[]{database,database+".backup",database+"-wal",database+"-shm",database+"-journal",database+".maxis-artifacts",Artifacts,Path.Combine(content,"models"),Path.Combine(content,"assets")} : new[]{scope!.Path};
        var files=new List<StorageFile>(); var issues=new List<string>(); var seen=new HashSet<string>(StringComparer.OrdinalIgnoreCase); var entries=0; var complete=true;
        void Issue(string text) { complete=false; if(issues.Count<32) issues.Add(text); }
        void Walk(string path,int depth)
        {
            token.ThrowIfCancellationRequested();
            if(++entries>2000) { Issue("Entry limit reached (2000); totals are a lower bound."); return; }
            try
            {
                path=DevelopmentPolicy.CheckedPath(path); if(!seen.Add(path)) return;
                var attributes=File.GetAttributes(path);
                if((attributes & FileAttributes.ReparsePoint)!=0) throw new IOException("Linked path.");
                if((attributes & FileAttributes.Directory)==0) { var f=new FileInfo(path); files.Add(new(path,f.Length,f.LastWriteTimeUtc)); }
                else
                {
                    if(depth>=32) { Issue("Depth limit reached: "+path); return; }
                    foreach(var child in Directory.EnumerateFileSystemEntries(path)) { if(entries>=2000) { Issue("Entry limit reached (2000); totals are a lower bound."); break; } Walk(child,depth+1); }
                }

            }
            catch(Exception e) when(e is FileNotFoundException or DirectoryNotFoundException) { if(!managed || path.Equals(database,StringComparison.OrdinalIgnoreCase) || !roots.Contains(path,StringComparer.OrdinalIgnoreCase)) Issue("Path vanished: "+path); }
            catch(Exception e) when(e is IOException or UnauthorizedAccessException or ArgumentException) { Issue("Unreadable/linked path: "+path); }
        }
        foreach(var path in roots) Walk(path,0);
        var duplicateGroups=new List<string[]>(); long hashBytes=0; var hashSkipped=0;
        if(task.Capability is "jn.duplicates" or "jn.cleanup")
        {
            foreach(var group in files.GroupBy(f=>f.Bytes).Where(g=>g.Count()>1))
            {
                var hashes=new Dictionary<string,List<string>>();
                foreach(var file in group)
                {
                    token.ThrowIfCancellationRequested();
                    if(file.Bytes>16*1024*1024 || hashBytes+file.Bytes>128*1024*1024) { hashSkipped++; continue; }
                    try
                    {
                        DevelopmentPolicy.CheckedPath(file.Path); var before=new FileInfo(file.Path);
                        if(before.Length!=file.Bytes || before.LastWriteTimeUtc!=file.LastWriteUtc) { hashSkipped++; continue; }
                        await using var stream=new FileStream(file.Path,FileMode.Open,FileAccess.Read,FileShare.Read,65536,true);
                        if(stream.Length!=file.Bytes) { hashSkipped++; continue; }
                        var hash=Convert.ToHexString(await SHA256.HashDataAsync(stream,token)); hashBytes+=file.Bytes;
                        var after=new FileInfo(file.Path); if(after.Length!=file.Bytes || after.LastWriteTimeUtc!=file.LastWriteUtc) { hashSkipped++; continue; }
                        if(!hashes.TryGetValue(hash,out var matches)) hashes[hash]=matches=[]; matches.Add(file.Path);
                    }
                    catch(Exception e) when(e is IOException or UnauthorizedAccessException) { hashSkipped++; }
                }
                duplicateGroups.AddRange(hashes.Values.Where(paths=>paths.Count>1).Select(paths=>paths.ToArray()));
            }
        }
        if(hashSkipped>0) Issue("Duplicate hashing skipped "+hashSkipped+" candidates (size/budget/change/access); review coverage.");
        token.ThrowIfCancellationRequested(); DevelopmentPolicy.CheckedPath(Artifacts);
        var output=DevelopmentPolicy.CheckedPath(Artifacts,task.Id.ToString("N")+"-"+(task.Attempts+1));
        if(Directory.Exists(output)) throw new IOException("Attempt report already exists; refusing overwrite.");
        Directory.CreateDirectory(output); var report=Path.Combine(output,"report.json"); var bytes=files.Sum(f=>f.Bytes);
        await File.WriteAllTextAsync(report,JsonSerializer.Serialize(new { observedUtc=DateTimeOffset.UtcNow,capability=task.Capability,roots,files,bytes,complete,issues,duplicateGroups,
            cleanup=task.Capability=="jn.cleanup" ? "PROPOSAL ONLY. Review duplicate groups manually. No preferred survivor, deletion, safe-to-remove verdict or guaranteed reclaim estimate." : null,
            quotaBytes=quota,requestedQuotaBytes=task.Capability=="jn.quota" ? (long?)long.Parse(task.Argument)*1_000_000_000 : null },new JsonSerializerOptions { WriteIndented=true }),token);
        var summary=$"[REAL STORAGE - READ ONLY] {files.Count} files; {bytes} bytes; coverage={(complete ? "complete within sampled roots" : "PARTIAL - lower bound")}.";
        if(managed) summary+=$" Logical GRID budget: {quota} bytes ({quota/1_000_000_000} decimal GB). "+(bytes>=quota ? "WARNING: measured usage meets/exceeds quota." : complete ? "Measured usage below quota." : "Incomplete measurement; cannot conclude usage is below quota.");
        if(task.Capability is "jn.duplicates" or "jn.cleanup") summary+=$" Possible duplicate groups: {duplicateGroups.Count}. No files deleted; equality does not imply safe deletion or reclaimable space.";
        return new JnReport(summary+" Report: "+report,bytes,complete);
    },token);
}
