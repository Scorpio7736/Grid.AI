using System.IO.Compression;
using System.Net;
using System.Text;
using System.Xml.Linq;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

namespace Grid.Terminal;

public sealed record GeneratedArtifact(byte[] Bytes, string PreviewHtml);

public static class GeneratedDocuments
{
    private static readonly XNamespace Word = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
    public static GeneratedArtifact Create(string extension, string title, string body)
    {
        if (extension is not (".pdf" or ".docx")) throw new ArgumentException("Choose a PDF or DOCX destination.");
        var text = title + "\n\n" + body.Replace("\r\n", "\n");
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(body) || text.Length > 10000 ||
            text.Any(c => c != '\n' && (c < 32 || c > 126)))
            throw new ArgumentException("The fixed report template accepts a title and body of up to 10,000 printable ASCII characters with line breaks.");
        var lines = new List<string>();
        foreach (var paragraph in text.Split('\n'))
        {
            var remaining = paragraph;
            while (remaining.Length > 72)
            {
                var space = remaining.LastIndexOf(' ', 71, 72);
                var count = space > 0 ? space : 72;
                lines.Add(remaining[..count]);
                remaining = remaining[count..].TrimStart(' ');
            }
            lines.Add(remaining);
        }
        var pages = lines.Chunk(54).ToArray();
        if (pages.Length > 8) throw new ArgumentException("The report exceeds eight pages.");
        var bytes = extension == ".pdf" ? Pdf(pages) : Docx(pages);
        Validate(bytes, extension, pages.Length);
        var preview = "<!doctype html><meta charset=utf-8><title>GRID report preview</title><style>body{background:#ccc}.page{box-sizing:border-box;width:612pt;height:792pt;padding:36pt;margin:12pt auto;background:white;white-space:pre;font:12pt/13pt Courier New,monospace} @media print{body{background:white}.page{margin:0;page-break-after:always}}</style>" +
            string.Join("", pages.Select(page => "<div class=page>" + WebUtility.HtmlEncode(string.Join("\n", page)) + "</div>"));
        return new(bytes, preview);
    }

    private static byte[] Pdf(string[][] pages)
    {
        var builder = new PdfDocumentBuilder();
        var font = builder.AddStandard14Font(Standard14Font.Courier);
        foreach (var lines in pages)
        {
            var page = builder.AddPage(612, 792);
            for (var i = 0; i < lines.Length; i++)
                if (lines[i].Length > 0) page.AddText(lines[i], 12, new PdfPoint(36, 744 - i * 13), font);
        }
        return builder.Build();
    }

    private static byte[] Docx(string[][] pages)
    {
        var body = new XElement(Word + "body");
        for (var page = 0; page < pages.Length; page++)
        {
            for (var line = 0; line < pages[page].Length; line++)
            {
                var properties = new XElement(Word + "pPr",
                    new XElement(Word + "spacing", new XAttribute(Word + "before", 0), new XAttribute(Word + "after", 0),
                        new XAttribute(Word + "line", 260), new XAttribute(Word + "lineRule", "exact")));
                if (page > 0 && line == 0) properties.Add(new XElement(Word + "pageBreakBefore"));
                body.Add(new XElement(Word + "p", properties,
                    new XElement(Word + "r", new XElement(Word + "rPr",
                        new XElement(Word + "rFonts", new XAttribute(Word + "ascii", "Courier New"), new XAttribute(Word + "hAnsi", "Courier New")),
                        new XElement(Word + "sz", new XAttribute(Word + "val", 24))),
                        new XElement(Word + "t", new XAttribute(XNamespace.Xml + "space", "preserve"), pages[page][line]))));
            }
        }
        body.Add(new XElement(Word + "sectPr", new XElement(Word + "pgSz", new XAttribute(Word + "w", 12240), new XAttribute(Word + "h", 15840)),
            new XElement(Word + "pgMar", new XAttribute(Word + "top", 720), new XAttribute(Word + "right", 720),
                new XAttribute(Word + "bottom", 720), new XAttribute(Word + "left", 720))));
        using var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            Add(zip, "[Content_Types].xml", "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Override PartName=\"/word/document.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml\"/></Types>");
            Add(zip, "_rels/.rels", "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"word/document.xml\"/></Relationships>");
            Add(zip, "word/document.xml", new XElement(Word + "document", new XAttribute(XNamespace.Xmlns + "w", Word), body).ToString());
        }
        return stream.ToArray();
    }

    private static void Add(ZipArchive zip, string name, string text)
    {
        var entry = zip.CreateEntry(name, CompressionLevel.Optimal);
        entry.LastWriteTime = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
        writer.Write(text);
    }

    private static void Validate(byte[] bytes, string extension, int pages)
    {
        if (bytes.Length > 1024 * 1024) throw new InvalidDataException("Generated output exceeds 1 MiB.");
        if (extension == ".pdf")
        {
            using var pdf = PdfDocument.Open(bytes);
            if (pdf.NumberOfPages != pages || pdf.GetPages().Any(page => page.Width != 612 || page.Height != 792))
                throw new InvalidDataException("Generated PDF failed page validation.");
            return;
        }
        using var zip = new ZipArchive(new MemoryStream(bytes));
        if (zip.Entries.Count != 3) throw new InvalidDataException("Unexpected document parts.");
        using var document = zip.GetEntry("word/document.xml")!.Open();
        if (XDocument.Load(document).Root?.Name != Word + "document") throw new InvalidDataException("Invalid Word document.");
    }
}
