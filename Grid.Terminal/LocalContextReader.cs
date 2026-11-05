using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Runtime.InteropServices;
using Grid.Communication;
namespace Grid.Terminal;

/// <summary>Reads one explicitly selected file. No folder grants, external relationships, macros or execution.</summary>
public static class LocalContextReader
{
    private static readonly HashSet<string> TextExtensions = new(StringComparer.OrdinalIgnoreCase) { ".txt", ".md", ".markdown", ".cs", ".csproj", ".sln", ".py", ".js", ".ts", ".tsx", ".jsx", ".json", ".xml", ".yaml", ".yml", ".toml", ".ini", ".csv", ".sql", ".html", ".css", ".cpp", ".c", ".h", ".java", ".rs", ".go", ".ps1", ".sh" };
    public static ContextDocument Read(string path, string project, Guid? agent, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!Path.IsPathFullyQualified(path) || path.StartsWith(@"\\", StringComparison.Ordinal) || path.Any(char.IsControl)) throw new ArgumentException("Select an absolute local file path; network/device paths are unsupported.");
        path = Path.GetFullPath(path); GridTransfer.CheckPath(path);
        if (new DriveInfo(Path.GetPathRoot(path)!).DriveType == DriveType.Network) throw new ArgumentException("Mapped network drives are not supported for offline context imports.");
        if (path.Length > 4096 || string.IsNullOrWhiteSpace(project) || project.Length > 80 || project.Any(char.IsControl)) throw new ArgumentException("Project name must be 1..80 printable characters; source path must be at most 4096 characters.");
        var extension = Path.GetExtension(path);
        if (!TextExtensions.Contains(extension) && !extension.Equals(".docx", StringComparison.OrdinalIgnoreCase) && !extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("Unsupported context format. Select UTF-8 text/Markdown/source code, PDF or DOCX; DOC, RTF and other binaries are not supported.");
        using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (OperatingSystem.IsWindows())
        {
            var resolved = new StringBuilder(32768);
            var length = GetFinalPathNameByHandle(file.SafeFileHandle, resolved, (uint)resolved.Capacity, 0);
            if (length == 0 || length >= resolved.Capacity || !resolved.ToString().Replace(@"\\?\", "").Equals(path, StringComparison.OrdinalIgnoreCase)) throw new IOException("Selected path does not match the opened file.");
        }
        GridTransfer.CheckPath(path);
        if (file.Length > ContextState.MaxSourceBytes) throw new ArgumentException("Context file exceeds the 1 MiB source limit; split or export a smaller file.");
        var modified = new DateTimeOffset(File.GetLastWriteTimeUtc(path));
        var bytes = ReadBounded(file, ContextState.MaxSourceBytes);
        var format = extension.ToLowerInvariant() switch { ".docx" => "docx-body", ".pdf" => "pdf-pages", _ => "utf8" };
        var text = format == "utf8" ? new UTF8Encoding(false, true).GetString(bytes.AsSpan(bytes.AsSpan().StartsWith(new byte[] { 239, 187, 191 }) ? 3 : 0))
            : DocumentExtraction.Extract(bytes, format, cancellationToken);
        var document = new ContextDocument(Guid.NewGuid(), project.Trim(), path, Convert.ToHexString(SHA256.HashData(bytes)), bytes.LongLength, modified, DateTimeOffset.UtcNow, format, text, agent);
        if (text.Length > ContextState.MaxTextCharacters) throw new InvalidDataException("Extracted context exceeds 256 Ki text characters; split the file. No content was truncated.");
        ContextState.Validate(new(new[] { document })); return document;
    }
    internal static byte[] ReadBounded(Stream stream, int limit)
    {
        using var result = new MemoryStream(); var buffer = new byte[8192]; int read;
        while ((read = stream.Read(buffer)) != 0)
        {
            if (result.Length + read > limit) throw new InvalidDataException("Context content exceeds its size limit.");
            result.Write(buffer, 0, read);
        }
        return result.ToArray();
    }
    internal static string Docx(byte[] bytes)
    {
        using var archive = new ZipArchive(new MemoryStream(bytes), ZipArchiveMode.Read);
        if (archive.Entries.Count > 256) throw new InvalidDataException("DOCX contains too many archive entries.");
        if (archive.Entries.Sum(entry => entry.Length) > 16L * ContextState.MaxSourceBytes) throw new InvalidDataException("DOCX expanded archive is too large; export a smaller document.");
        var entries = archive.Entries.Where(e => e.FullName == "word/document.xml").ToArray();
        if (entries.Length != 1 || entries[0].Length > ContextState.MaxSourceBytes) throw new InvalidDataException("DOCX body is missing, duplicated or too large.");
        using var part = entries[0].Open(); var xml = ReadBounded(part, ContextState.MaxSourceBytes);
        using var reader = XmlReader.Create(new MemoryStream(xml), new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = ContextState.MaxSourceBytes });
        var document = XDocument.Load(reader);
        XNamespace w = document.Root?.Name.Namespace ?? XNamespace.None;
        if (w.NamespaceName is not ("http://schemas.openxmlformats.org/wordprocessingml/2006/main" or "http://purl.oclc.org/ooxml/wordprocessingml/main"))
            throw new InvalidDataException("Unsupported DOCX namespace.");
        if (document.Root?.Name != w + "document") throw new InvalidDataException("Unsupported DOCX document body.");
        var output = new StringBuilder();
        var body = document.Root.Element(w + "body") ?? throw new InvalidDataException("DOCX body is missing.");
        if (body.Descendants().Any(node => node.Name == w + "altChunk" || node.Name == w + "subDoc"))
            throw new InvalidDataException("DOCX contains linked/embedded body content; export a flattened document before importing.");
        var paragraphNumber = 0;
        var hasText = false;
        foreach (var paragraph in body.Descendants(w + "p"))
        {
            output.AppendLine($"[DOCX body paragraph {++paragraphNumber}]");
            if (paragraph.Ancestors(w + "tc").FirstOrDefault() is { } cell && cell.Parent is { } row)
                output.AppendLine($"[Table row {row.ElementsBeforeSelf(w + "tr").Count() + 1}, cell {cell.ElementsBeforeSelf(w + "tc").Count() + 1}]");
            foreach (var node in paragraph.Descendants().Where(n => n.Ancestors(w + "p").FirstOrDefault() == paragraph))
            {
                if (node.Name == w + "t") { output.Append(node.Value); hasText |= !string.IsNullOrWhiteSpace(node.Value); }
                else if (node.Name == w + "tab") output.Append('\t');
                else if (node.Name == w + "br") output.Append('\n');
                if (output.Length > ContextState.MaxTextCharacters) throw new InvalidDataException("Extracted DOCX text exceeds 256 Ki characters.");
            }
            output.AppendLine();
        }
        if (!hasText) throw new InvalidDataException("DOCX body contains no extractable text; export paragraphs/tables as text and retry.");
        return output.ToString();
    }
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint GetFinalPathNameByHandle(Microsoft.Win32.SafeHandles.SafeFileHandle handle, StringBuilder path, uint size, uint flags);
}
