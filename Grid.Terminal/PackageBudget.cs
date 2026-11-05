using System.Security.Cryptography;
using System.Text.Json;
namespace Grid.Terminal;

public sealed record PackageFootprint(long BaseBytes, long MutableBytes, long StagingBytes, int FileCount)
{
    public long TotalBytes => checked(BaseBytes + MutableBytes + StagingBytes);
    public long BaseHeadroom => PackageBudget.Limit - BaseBytes;
    public long UpdateFreeSpaceRequired => checked(BaseBytes + PackageBudget.Reserve);
    public bool WithinLimit => BaseBytes <= PackageBudget.Limit;
    public override string ToString() => $"Base: {BaseBytes:N0} / {PackageBudget.Limit:N0} bytes; headroom: {BaseHeadroom:N0}\nMutable saves/context/logs/recovery: {MutableBytes:N0} bytes\nStaging/old versions: {StagingBytes:N0} bytes\nTotal footprint: {TotalBytes:N0} bytes in {FileCount} files\nFull replacement staging requires {UpdateFreeSpaceRequired:N0} additional free bytes (base + reserve).";
}
public static class PackageBudget
{
    public const long Limit = 20_000_000_000;
    public const long Reserve = 256_000_000;
    public static string Category(string relative)
    {
        var path = relative.Replace('\\','/').ToLowerInvariant();
        var first = path.Split('/')[0];
        if (first is "staging" or "old-versions" || first.Contains(".staging-")) return "staging";
        if (path is "runtime/model-registry.json" or "runtime/model-registry.lock" || path.StartsWith("runtime/model-registry.json.tmp-")) return "mutable";
        if (path.StartsWith("data/") || path.StartsWith("runtime/temp/") || path.StartsWith("runtime/home/") || path == "runtime/ai-runtime.log") return "mutable";
        return "base"; // Unknown assets, source, extraction libraries and nested archives count toward the ceiling.
    }
    public static PackageFootprint Measure(string root)
    {
        root = Path.GetFullPath(root); GridTransfer.CheckPath(root);
        long baseBytes=0, mutable=0, staging=0; int count=0;
        void Walk(string directory)
        {
            if(File.GetAttributes(directory).HasFlag(FileAttributes.ReparsePoint)) throw new IOException("Package directory links are not supported: "+directory);
            foreach (var path in Directory.EnumerateFiles(directory))
            {
                if(File.GetAttributes(path).HasFlag(FileAttributes.ReparsePoint)) throw new IOException("Package file links are not supported: "+path); var length = new FileInfo(path).Length;
                switch(Category(Path.GetRelativePath(root,path))) {
                    case "mutable": mutable=checked(mutable+length); break;
                    case "staging": staging=checked(staging+length); break;
                    default: baseBytes=checked(baseBytes+length); break;
                } count++;
            }
            foreach(var child in Directory.EnumerateDirectories(directory)) Walk(child);
        }
        Walk(root); return new(baseBytes,mutable,staging,count);
    }
    public static void RequireBase(PackageFootprint size)
    { if (!size.WithinLimit) throw new IOException($"Base package exceeds {Limit:N0} bytes by {-size.BaseHeadroom:N0}. No completed package may be published."); }
    public static void VerifyModel(string runtime, bool hashes, IReadOnlyDictionary<string,string>? verifiedHashes = null)
    {
        GridTransfer.CheckPath(runtime);
        var manifestPath=Path.Combine(runtime,"models","manifests","registry.ollama.ai","library","qwen3","4b");
        var manifestBytes=File.ReadAllBytes(manifestPath);
        const string manifestHash="31D9857B5705086D352D4D6DB04E86C5D0F6A2B5D59A4DC7CE5CDB271CCF9141";
        if(Convert.ToHexString(SHA256.HashData(manifestBytes))!=manifestHash) throw new InvalidDataException("Unbenchmarked model manifest. Restore the pinned qwen3:4b assets; model tags alone do not identify versions.");
        using var doc=JsonDocument.Parse(manifestBytes);
        foreach(var blob in doc.RootElement.GetProperty("layers").EnumerateArray().Prepend(doc.RootElement.GetProperty("config")))
        {
            var digest=blob.GetProperty("digest").GetString()!;
            if(!System.Text.RegularExpressions.Regex.IsMatch(digest,@"^sha256:[0-9a-f]{64}$")) throw new InvalidDataException("Invalid model digest.");
            var file=Path.Combine(runtime,"models","blobs",digest.Replace(':','-')); GridTransfer.CheckPath(file);
            if(!File.Exists(file)||new FileInfo(file).Length!=blob.GetProperty("size").GetInt64()) throw new InvalidDataException("Missing/wrong-sized shared model blob: "+digest);
            if(verifiedHashes is not null) {
                if(!verifiedHashes.TryGetValue(Path.GetFullPath(file),out var verified) || verified!=digest[7..].ToUpperInvariant()) throw new InvalidDataException("Unverified/corrupt model blob: "+digest);
            } else if(hashes && GridTransfer.Hash(file)!=digest[7..].ToUpperInvariant()) throw new InvalidDataException("Corrupt model blob: "+digest);
        }
    }
    public static int Audit(string root)
    {
        try { var size=Measure(root); Console.WriteLine(size); RequireBase(size); VerifyModel(Path.Combine(root,"runtime"),true); Console.WriteLine("Pinned shared model assets verified. No model copies per role are required."); return 0; }
        catch(Exception e) when(e is IOException or UnauthorizedAccessException or JsonException) { Console.Error.WriteLine("Package audit failed: "+e.Message); return 1; }
    }
}
