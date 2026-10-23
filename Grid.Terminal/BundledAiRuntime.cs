using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Grid.Communication;
namespace Grid.Terminal;

/// <summary>Owns only the bundled child server. Never discovers or stops a user's global Ollama.</summary>
public sealed class BundledAiRuntime : IAsyncDisposable
{
    private Process? process;
    public Uri Endpoint { get; private set; } = new("http://127.0.0.1:1/");
    public bool Ready { get; private set; }
    public string Status { get; private set; } = "Local AI not started.";
    public static string? FindRoot(string start)
    {
        var packaged = Path.GetFullPath(Path.Combine(start, "..", "runtime"));
        if (File.Exists(Path.Combine(start,"..",GridTransfer.Marker))) return packaged;
        for (var dir = new DirectoryInfo(start); dir is not null; dir = dir.Parent)
            if (File.Exists(Path.Combine(dir.FullName,"Grid7736.slnx"))) return Path.Combine(dir.FullName,".grid-runtime");
        return null;
    }
    public static async Task<BundledAiRuntime> StartAsync(string? root, CancellationToken token = default)
    {
        var runtime = new BundledAiRuntime();
        if (Environment.GetEnvironmentVariable("GRID_AI_DISABLED") == "1")
        { runtime.Status = "Local AI disabled by GRID_AI_DISABLED=1."; return runtime; }
        if (root is null || !File.Exists(Path.Combine(root,"assets.json")) || !File.Exists(Path.Combine(root,"ollama","ollama.exe")) ||
            !File.Exists(Path.Combine(root,"models","manifests","registry.ollama.ai","library","qwen3","4b")))
        { runtime.Status = "Local AI assets missing. Run Prepare-GridRuntime.ps1 in the source repository, or use a complete portable package. No global Ollama fallback."; return runtime; }
        try
        {
            var listener = new TcpListener(IPAddress.Loopback,0); listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port; listener.Stop();
            runtime.Endpoint = new($"http://127.0.0.1:{port}/");
            var start = new ProcessStartInfo(Path.Combine(root,"ollama","ollama.exe"),"serve")
            { UseShellExecute=false, CreateNoWindow=true, WindowStyle=ProcessWindowStyle.Hidden, WorkingDirectory=root, RedirectStandardOutput=true, RedirectStandardError=true };
            start.Environment["OLLAMA_HOST"]=$"127.0.0.1:{port}";
            start.Environment["OLLAMA_MODELS"]=Path.Combine(root,"models");
            start.Environment["OLLAMA_NO_CLOUD"]="1";
            start.Environment["OLLAMA_NUM_PARALLEL"]="1";
            start.Environment["OLLAMA_MAX_LOADED_MODELS"]="1";
            start.Environment["OLLAMA_CONTEXT_LENGTH"]="4096";
            start.Environment["OLLAMA_KEEP_ALIVE"]="1m";
            var temp = Path.Combine(root,"temp");Directory.CreateDirectory(temp);
            start.Environment["TEMP"]=temp;start.Environment["TMP"]=temp;
            var home=Path.Combine(root,"home");Directory.CreateDirectory(home);
            start.Environment["HOME"]=home;start.Environment["USERPROFILE"]=home;
            start.Environment["LOCALAPPDATA"]=Path.Combine(home,"AppData","Local");
            var log=Path.Combine(root,"ai-runtime.log");var logGate=new object();
            runtime.process=Process.Start(start) ?? throw new IOException("Bundled AI process did not start.");
            void Log(object sender, DataReceivedEventArgs args)
            {
                if (args.Data is null) return;
                lock(logGate) try { if (!File.Exists(log) || new FileInfo(log).Length < 5*1024*1024) File.AppendAllText(log,args.Data + Environment.NewLine); } catch(Exception e) when(e is IOException or UnauthorizedAccessException) { }
            }
            runtime.process.OutputDataReceived+=Log;runtime.process.ErrorDataReceived+=Log;
            runtime.process.BeginOutputReadLine();runtime.process.BeginErrorReadLine();
            using var deadline=CancellationTokenSource.CreateLinkedTokenSource(token);deadline.CancelAfter(TimeSpan.FromSeconds(30));
            using var backend=new OllamaLocalBackend(endpoint:runtime.Endpoint);
            while (!runtime.process.HasExited)
            {
                deadline.Token.ThrowIfCancellationRequested();
                var status=await backend.StatusAsync(deadline.Token);
                if (status.Contains("Installed: True",StringComparison.Ordinal))
                { runtime.Ready=true;runtime.Status="Bundled local AI ready: qwen3:4b. Runtime/model files: " + root;return runtime; }
                await Task.Delay(250,deadline.Token);
            }
            throw new IOException("Bundled AI exited before readiness; check bundled libraries and hardware compatibility.");
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception or OperationCanceledException or SocketException)
        {
            await runtime.DisposeAsync();runtime.Status="Bundled local AI unavailable: " + e.Message;
            token.ThrowIfCancellationRequested();return runtime;
        }
    }
    public async ValueTask DisposeAsync()
    {
        Ready=false;
        if (process is null) return;
        try { if (!process.HasExited) { process.Kill(entireProcessTree:true);await process.WaitForExitAsync(); } }
        catch (InvalidOperationException) { }
        finally { process.Dispose();process=null; }
    }
}
