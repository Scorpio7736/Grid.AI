using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace Grid.AI;

/// <summary>One resource owner across generation, streaming and lifecycle changes.</summary>
public sealed class LocalModelSession : IAsyncDisposable
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly CancellationTokenSource shutdown = new();
    private ILocalModelProvider? provider;
    private LocalModel? model;
    private int disposed;

    public async Task SwitchAsync(ILocalModelProvider next, LocalModel selected, CancellationToken token = default)
    {
        if (!Enum.IsDefined(selected.Kind) || selected.Kind == ModelOutputKind.Image && !next.Capabilities.Images ||
            selected.Kind == ModelOutputKind.Text && !next.Capabilities.Text)
            throw new NotSupportedException("Provider cannot load the requested model capability.");
        using var stop = Deadline(TimeSpan.FromMinutes(2), token);
        await gate.WaitAsync(stop.Token);
        try
        {
            if (provider is not null && model is not null) await provider.UnloadAsync(model, stop.Token);
            provider = next; model = selected;
            try { await next.LoadAsync(selected, stop.Token); }
            catch
            {
                using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                await next.UnloadAsync(selected, cleanup.Token);
                provider = null; model = null;
                throw;
            }
        }
        finally { gate.Release(); }
    }

    public async Task<ModelResult> GenerateAsync(ModelRequest request, CancellationToken token = default)
    {
        using var stop = Deadline(request.Timeout, token);
        await gate.WaitAsync(stop.Token);
        try
        {
            var active = provider ?? throw new InvalidOperationException("Load a local model first.");
            request.Validate(active.Capabilities);
            if (request.Kind != model!.Kind) throw new ArgumentException("Request kind differs from the loaded model.");
            var result = await active.GenerateAsync(model!, request, stop.Token);
            stop.Token.ThrowIfCancellationRequested();
            return result;
        }
        finally { gate.Release(); }
    }

    public async IAsyncEnumerable<ModelChunk> StreamAsync(ModelRequest request, [EnumeratorCancellation] CancellationToken token = default)
    {
        using var stop = Deadline(request.Timeout, token);
        var channel = Channel.CreateBounded<ModelChunk>(new BoundedChannelOptions(16)
            { SingleReader = true, SingleWriter = true, FullMode = BoundedChannelFullMode.Wait });
        var producer = ProduceStreamAsync(request, channel.Writer, stop.Token);
        try
        {
            await foreach (var chunk in channel.Reader.ReadAllAsync(stop.Token)) yield return chunk;
        }
        finally
        {
            stop.Cancel();
            await producer;
        }
    }

    private async Task ProduceStreamAsync(ModelRequest request, ChannelWriter<ModelChunk> writer, CancellationToken token)
    {
        var acquired = false;
        try
        {
            await gate.WaitAsync(token); acquired = true;
            var active = provider ?? throw new InvalidOperationException("Load a local model first.");
            request.Validate(active.Capabilities);
            if (request.Kind != model!.Kind) throw new ArgumentException("Request kind differs from the loaded model.");
            if (!active.Capabilities.Streaming) throw new NotSupportedException("Streaming is unavailable.");
            ModelChunk? completion = null;
            await foreach (var chunk in active.StreamAsync(model!, request, token))
            {
                token.ThrowIfCancellationRequested();
                if (completion is not null) throw new InvalidDataException("Provider sent data after stream completion.");
                if (chunk.Finished) completion = chunk;
                else await writer.WriteAsync(chunk, token);
            }
            if (completion is null) throw new InvalidDataException("Model stream ended without completion.");
            await writer.WriteAsync(completion, token);
            writer.TryComplete();
        }
        catch (Exception exception) { writer.TryComplete(exception); }
        finally { if (acquired) gate.Release(); }
    }

    public async Task<ModelResources> UnloadAsync(CancellationToken token = default)
    {
        using var stop = Deadline(TimeSpan.FromSeconds(30), token);
        await gate.WaitAsync(stop.Token);
        try
        {
            if (provider is null || model is null) return new(false, 0, 0, "Session has no loaded model.");
            await provider.UnloadAsync(model, stop.Token);
            var resources = await provider.ResourcesAsync(model, stop.Token);
            if (resources.Loaded) throw new IOException("Provider still reports a loaded model after unload.");
            provider = null; model = null;
            return resources;
        }
        finally { gate.Release(); }
    }

    private CancellationTokenSource Deadline(TimeSpan timeout, CancellationToken token)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref disposed) != 0, this);
        if (timeout <= TimeSpan.Zero || timeout > TimeSpan.FromMinutes(5)) throw new ArgumentOutOfRangeException(nameof(timeout));
        var stop = CancellationTokenSource.CreateLinkedTokenSource(token, shutdown.Token);
        stop.CancelAfter(timeout);
        return stop;
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0) return;
        shutdown.Cancel();
        await gate.WaitAsync();
        try
        {
            if (provider is not null && model is not null)
            {
                using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                await provider.UnloadAsync(model, stop.Token);
            }
            provider = null; model = null;
        }
        finally { gate.Release(); }
        // Provider ownership belongs to the caller so a session can switch between reusable providers.
    }
}
