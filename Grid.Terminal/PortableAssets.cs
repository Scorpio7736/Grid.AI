using System.Text.Json;
using System.Runtime.InteropServices;
namespace Grid.Terminal;

public sealed record PortableAsset(string Path,long Length,string Sha256);
public static class PortableAssets
{
    public const string Manifest="portable-assets.json";
    public static string Launcher => """
@echo off
setlocal DisableDelayedExpansion
set "DOTNET_ROOT=%~dp0runtime\dotnet"
set "DOTNET_ROOT_X64=%~dp0runtime\dotnet"
set "DOTNET_MULTILEVEL_LOOKUP=0"
"%~dp0app\Grid.Terminal.exe" %*
exit /b %errorlevel%
""".Replace("\n", "\r\n") + "\r\n";
    public static void ValidateRuntime(string runtime,bool hashes, IReadOnlyDictionary<string,string>? verifiedHashes = null)
    {
        foreach(var relative in new[]{"assets.json","ollama/ollama.exe","dotnet/dotnet.exe"}) if(!File.Exists(Path.Combine(runtime,relative))) throw new IOException("Required portable runtime asset missing: "+relative);
        foreach(var relative in new[]{"ollama/lib","dotnet/host/fxr","dotnet/shared/Microsoft.NETCore.App","dotnet/sdk"}) {
            var path=Path.Combine(runtime,relative);GridTransfer.CheckPath(path);
            if(!Directory.Exists(path)||!Directory.EnumerateFileSystemEntries(path).Any()) throw new IOException("Required portable runtime directory missing: "+relative);
        }
        PackageBudget.VerifyModel(runtime,hashes,verifiedHashes);
    }
    public static bool RequiresManifest(string root)
    {
        using var marker=JsonDocument.Parse(File.ReadAllText(Path.Combine(root,GridTransfer.Marker)));
        return marker.RootElement.TryGetProperty("version",out var version) && version.GetInt32()>=2;
    }
    public static void Validate(string root,bool hashes) => ValidateCore(root,hashes,null,null);
    internal static void ValidateTransferred(string root,IEnumerable<TransferFile> files,Action<string>? progress = null)
    {
        var verified=files.ToDictionary(f=>Path.GetFullPath(Path.Combine(root,f.Relative)),f=>f.Hash,StringComparer.OrdinalIgnoreCase);
        ValidateCore(root,false,verified,progress);
    }
    private static void ValidateCore(string root,bool hashes,IReadOnlyDictionary<string,string>? verified,Action<string>? progress)
    {
        if(!OperatingSystem.IsWindows() || RuntimeInformation.OSArchitecture!=Architecture.X64) throw new IOException("This package supports Windows x64 only.");
        void Required(string relative) { var file=Path.Combine(root,relative); GridTransfer.CheckPath(file); if(!File.Exists(file)) throw new IOException("Required portable asset missing: "+relative); }
        foreach(var path in new[]{"app/Grid.Terminal.exe","app/Grid.Terminal.dll","app/Grid.Terminal.runtimeconfig.json","runtime/assets.json","runtime/ollama/ollama.exe","runtime/dotnet/dotnet.exe"}) Required(path);
        foreach(var path in new[]{"runtime/ollama/lib","runtime/dotnet/host/fxr","runtime/dotnet/shared/Microsoft.NETCore.App","runtime/dotnet/sdk"})
            if(!Directory.Exists(Path.Combine(root,path)) || !Directory.EnumerateFileSystemEntries(Path.Combine(root,path)).Any()) throw new IOException("Required runtime directory missing/empty: "+path);
        ValidateRuntime(Path.Combine(root,"runtime"),hashes,verified); PackageBudget.RequireBase(PackageBudget.Measure(root));
        if(RequiresManifest(root) && !File.Exists(Path.Combine(root,Manifest))) throw new IOException("Portable asset manifest missing; package is incomplete.");
        if(File.Exists(Path.Combine(root,Manifest))) {
            var entries=JsonSerializer.Deserialize<PortableAsset[]>(File.ReadAllText(Path.Combine(root,Manifest))) ?? throw new InvalidDataException("Missing asset inventory.");
            ValidateEntries(root,entries,hashes,verified,progress);
        }
    }
    internal static void ValidateEntries(string root,IReadOnlyList<PortableAsset> entries,bool hashes,IReadOnlyDictionary<string,string>? verified = null,Action<string>? progress = null)
    {
        if(entries.Count>200000 || entries.Select(e=>e.Path).Distinct(StringComparer.OrdinalIgnoreCase).Count()!=entries.Count) throw new InvalidDataException("Invalid asset inventory count/duplicates.");
        foreach(var entry in entries) {
            progress?.Invoke(entry.Path);
            if(string.IsNullOrWhiteSpace(entry.Path)||entry.Length<0) throw new InvalidDataException("Invalid asset inventory entry.");
            var path=Path.GetFullPath(Path.Combine(root,entry.Path));
            if(Path.IsPathRooted(entry.Path)||!GridTransfer.Within(path,Path.GetFullPath(root))) throw new InvalidDataException("Invalid relative asset path.");
            GridTransfer.CheckPath(path);
            if(!File.Exists(path)||new FileInfo(path).Length!=entry.Length || hashes && GridTransfer.Hash(path)!=entry.Sha256) throw new IOException("Portable asset missing/changed: "+entry.Path);
            if(verified is not null && (!verified.TryGetValue(path,out var hash) || hash!=entry.Sha256)) throw new IOException("Portable asset was not verified during copying: "+entry.Path);
        }
    }

    public static void Seal(string root)
    {
        GridTransfer.CheckPath(root);
        var files=Directory.EnumerateFiles(root,"*",SearchOption.AllDirectories).Where(p=>PackageBudget.Category(Path.GetRelativePath(root,p))=="base" && Path.GetFileName(p)!=Manifest)
            .Select(p=>{ GridTransfer.CheckPath(p);return new TransferFile(p,Path.GetRelativePath(root,p),new FileInfo(p).Length,GridTransfer.Hash(p)); }).ToArray();
        WriteManifest(root,files); Validate(root,true);
    }
    public static void WriteManifest(string root,IEnumerable<TransferFile> files)
    {
        var entries=files.Where(f=>PackageBudget.Category(f.Relative)=="base" && f.Relative!=Manifest && f.Relative!="transfer-manifest.json" && !f.Relative.Replace('\\','/').StartsWith("transfer-"))
            .Select(f=>new PortableAsset(f.Relative.Replace('\\','/'),f.Length,f.Hash)).OrderBy(f => f.Path, StringComparer.Ordinal).ToArray();
        File.WriteAllText(Path.Combine(root,Manifest),JsonSerializer.Serialize(entries,new JsonSerializerOptions{WriteIndented=true}));
    }
    public static string? SourceRoot(string app)
    {
        for(var dir=new DirectoryInfo(app);dir is not null;dir=dir.Parent) if(File.Exists(Path.Combine(dir.FullName,"Grid.AI.slnx"))) return dir.FullName;
        return null;
    }
    public static IEnumerable<string> SourceFiles(string root)
    {
        var excluded=new HashSet<string>(new[]{".git",".vs",".grid-runtime","portable","bin","obj","TestResults","data","runtime","app","node_modules"},StringComparer.OrdinalIgnoreCase);
        IEnumerable<string> Walk(string directory) {
            GridTransfer.CheckPath(directory);
            foreach(var file in Directory.EnumerateFiles(directory)) if(new[]{".cs",".csproj",".slnx",".props",".targets",".ps1",".md",".json"}.Contains(Path.GetExtension(file),StringComparer.OrdinalIgnoreCase) || Path.GetFileName(file) is "Ollama-LICENSE" or "PdfPig-LICENSE" or "llama.cpp-LICENSE") yield return file;
            foreach(var child in Directory.EnumerateDirectories(directory)) if(!excluded.Contains(Path.GetFileName(child))) foreach(var file in Walk(child)) yield return file;
        }
        return Walk(root);
    }
}
