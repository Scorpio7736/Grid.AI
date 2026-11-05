using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace Grid.AI;

public sealed class LocalHttp : IDisposable
{
    private readonly HttpClient client;
    public LocalHttp(Uri endpoint, HttpMessageHandler? handler = null)
    {
        if (endpoint.Scheme != "http" || endpoint.Host != "127.0.0.1" || endpoint.AbsolutePath != "/" ||
            endpoint.UserInfo.Length != 0 || endpoint.Query.Length != 0 || endpoint.Fragment.Length != 0)
            throw new ArgumentException("Local inference requires an IPv4 loopback root endpoint.");
        client = new(handler ?? new HttpClientHandler { UseProxy = false, AllowAutoRedirect = false })
            { BaseAddress = endpoint, Timeout = System.Threading.Timeout.InfiniteTimeSpan };
    }
    public async Task<JsonDocument> JsonAsync(HttpMethod method, string path, object? body, CancellationToken token)
    {
        using var request = new HttpRequestMessage(method, path);
        if (body is not null) request.Content = JsonContent.Create(body);
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(token);
        using var bytes = new MemoryStream();
        var buffer = new byte[8192];
        int count;
        while ((count = await stream.ReadAsync(buffer, token)) != 0)
        {
            if (bytes.Length + count > 1024 * 1024) throw new InvalidDataException("Local AI response exceeds limit.");
            bytes.Write(buffer, 0, count);
        }
        return JsonDocument.Parse(bytes.ToArray());
    }
    public async IAsyncEnumerable<string> LinesAsync(string path, object body, [EnumeratorCancellation] CancellationToken token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(body) };
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(token);
        using var reader = new StreamReader(stream, new UTF8Encoding(false, true));
        var buffer = new char[4096];
        var line = new StringBuilder();
        var total = 0;
        int count;
        while ((count = await reader.ReadAsync(buffer, token)) != 0)
        {
            total += count;
            if (total > 2 * 1024 * 1024) throw new InvalidDataException("Model stream exceeds limit.");
            for (var i = 0; i < count; i++)
            {
                if (buffer[i] == '\n') { yield return line.ToString().TrimEnd('\r'); line.Clear(); }
                else line.Append(buffer[i]);
                if (line.Length > 256 * 1024) throw new InvalidDataException("Model stream frame exceeds limit.");
            }
        }
        if (line.Length > 0) yield return line.ToString();
    }
    public void Dispose() => client.Dispose();
}
