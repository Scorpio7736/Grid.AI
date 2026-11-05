using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Grid.Communication;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using UglyToad.PdfPig.Exceptions;

namespace Grid.Terminal;

/// <summary>Bounded parser process. Receives captured bytes, never a source path or world credentials.</summary>
internal static class DocumentExtraction
{
    internal const int MaxPages = 200;
    internal const long MaxWorkingSet = 512L * 1024 * 1024;
    internal static readonly TimeSpan Timeout = TimeSpan.FromSeconds(20);
    private sealed record Result(string? Text, string? Error);

    internal static string Extract(byte[] bytes, string format, CancellationToken cancellationToken)
        => ExtractAsync(bytes, format, cancellationToken).GetAwaiter().GetResult();

    private static async Task<string> ExtractAsync(byte[] bytes, string format, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var assembly = typeof(DocumentExtraction).Assembly.Location;
        var executable = Path.ChangeExtension(assembly, OperatingSystem.IsWindows() ? ".exe" : null);
        var start = new ProcessStartInfo(executable)
        {
            UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden,
            RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true
        };
        start.ArgumentList.Add("--extract-document");
        start.ArgumentList.Add(format);
        start.Environment["DOTNET_GCHeapHardLimit"] = "10000000"; // 256 MiB managed heap, hexadecimal.
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(Timeout);
        using var process = Process.Start(start) ?? throw new IOException("Document extraction worker could not start.");
        try
        {
            var output = ReadLimitedAsync(process.StandardOutput, 2 * 1024 * 1024, deadline.Token);
            var error = ReadLimitedAsync(process.StandardError, 8192, deadline.Token);
            await process.StandardInput.BaseStream.WriteAsync(bytes, deadline.Token);
            process.StandardInput.Close();
            while (!process.HasExited)
            {
                if (output.IsFaulted) await output;
                if (error.IsFaulted) await error;
                try
                {
                    process.Refresh();
                    if (process.WorkingSet64 > MaxWorkingSet)
                        throw new InvalidDataException("Document extraction exceeded its memory limit; split or export a smaller document.");
                }
                catch (InvalidOperationException) when (process.HasExited) { break; }
                await Task.Delay(25, deadline.Token);
            }
            var response = await output;
            await error;
            if (process.ExitCode != 0) throw new InvalidDataException("Document parser stopped unexpectedly; the document was not imported.");
            var result = JsonSerializer.Deserialize<Result>(response) ?? throw new InvalidDataException("Missing extraction result.");
            if (result.Error is not null) throw new InvalidDataException(result.Error);
            if (string.IsNullOrWhiteSpace(result.Text) || result.Text.Length > ContextState.MaxTextCharacters)
                throw new InvalidDataException("Document extraction returned empty or oversized text.");
            cancellationToken.ThrowIfCancellationRequested();
            return result.Text;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new InvalidDataException("Document extraction exceeded 20 seconds; split or export a smaller document. Nothing imported.");
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("Invalid document extraction result.", exception);
        }
        finally
        {
            try { if (!process.HasExited) process.Kill(entireProcessTree: true); }
            catch (InvalidOperationException) when (process.HasExited) { }
            await process.WaitForExitAsync();
        }
    }

    private static async Task<string> ReadLimitedAsync(StreamReader reader, int limit, CancellationToken token)
    {
        var text = new StringBuilder();
        var buffer = new char[8192];
        int count;
        while ((count = await reader.ReadAsync(buffer.AsMemory(), token)) != 0)
        {
            if (text.Length + count > limit) throw new InvalidDataException("Document parser output exceeded its limit.");
            text.Append(buffer, 0, count);
        }
        return text.ToString();
    }

    internal static int RunWorker(string format)
    {
        Result result;
        try
        {
            using var input = Console.OpenStandardInput();
            var bytes = LocalContextReader.ReadBounded(input, ContextState.MaxSourceBytes);
            var text = format switch
            {
                "pdf-pages" => Pdf(bytes),
                "docx-body" => LocalContextReader.Docx(bytes),
                _ => throw new InvalidDataException("Unsupported document format.")
            };
            result = new(text, null);
        }
        catch (Exception exception)
        {
            var message = exception switch
            {
                PdfDocumentEncryptedException => "Encrypted/password-protected PDF. Export an unlocked text PDF before importing.",
                InvalidDataException or System.Xml.XmlException => exception.Message,
                _ => "Invalid, corrupt or unsupported document. Export a valid PDF/DOCX or UTF-8 text file."
            };
            result = new(null, message);
        }
        Console.Write(JsonSerializer.Serialize(result));
        return 0;
    }

    internal static string Pdf(byte[] bytes)
    {
        if (!bytes.AsSpan().StartsWith("%PDF-"u8)) throw new InvalidDataException("File does not have a PDF signature.");
        using var document = PdfDocument.Open(bytes, new ParsingOptions { UseLenientParsing = false, SkipMissingFonts = false });
        if (document.IsEncrypted) throw new InvalidDataException("Encrypted PDF. Export an unlocked text PDF before importing.");
        if (document.NumberOfPages is < 1 or > MaxPages) throw new InvalidDataException("PDF must contain 1..200 pages; split the document.");
        var text = new StringBuilder();
        var hasText = false;
        for (var number = 1; number <= document.NumberOfPages; number++)
        {
            var page = document.GetPage(number);
            var content = ContentOrderTextExtractor.GetText(page);
            text.AppendLine($"[PDF page {number} of {document.NumberOfPages}]");
            if (string.IsNullOrWhiteSpace(content))
                text.AppendLine("[No extractable text on this page. It may be blank or scanned; OCR is not included.]");
            else
            {
                hasText = true;
                text.AppendLine(content);
            }
            if (text.Length > ContextState.MaxTextCharacters) throw new InvalidDataException("Extracted PDF exceeds 256 Ki characters; split the document. Nothing truncated.");
        }
        if (!hasText) throw new InvalidDataException("PDF has no extractable text. It may be scanned or blank. Use offline OCR or export a text PDF, then retry; nothing imported.");
        return text.ToString();
    }
}
