using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using Grid.Communication;
using Grid.Core;
using Grid.Persistence;
using Grid.Simulation;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;
using Xunit;

namespace Grid.Terminal.Tests;

public sealed class DocumentExtractionTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "GridDocuments-" + Guid.NewGuid());
    public DocumentExtractionTests() => Directory.CreateDirectory(root);
    public void Dispose() => Directory.Delete(root, true);

    private string Pdf(params string[] pages)
    {
        var builder = new PdfDocumentBuilder();
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);
        foreach (var text in pages)
        {
            var page = builder.AddPage(400, 500);
            if (text.Length > 0) page.AddText(text, 12, new PdfPoint(20, 450), font);
        }
        var path = Path.Combine(root, Guid.NewGuid() + ".pdf");
        File.WriteAllBytes(path, builder.Build());
        return path;
    }

    private string Docx(string body)
    {
        var path = Path.Combine(root, Guid.NewGuid() + ".docx");
        using var archive = ZipFile.Open(path, ZipArchiveMode.Create);
        using var writer = new StreamWriter(archive.CreateEntry("word/document.xml").Open());
        writer.Write("<w:document xmlns:w='http://schemas.openxmlformats.org/wordprocessingml/2006/main'><w:body>" + body + "</w:body></w:document>");
        return path;
    }

    [Fact]
    public void PdfImportsThroughCommandsAndPersistsCapturedPageProvenance()
    {
        var registry = new TerminalSessionRegistry();
        var identity = new TerminalIdentitySession(() => "pdf-test");
        registry.Add(identity);
        var engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis(), editAuthorizer: registry);
        identity.Attach(engine); identity.Login();
        var commands = DefaultCommands.Create(engine);
        ContextCommands.Register(commands, engine, identity, () => { }); commands.UseIdentity(identity);
        var path = Pdf("First page fact", "Second page fact");
        Assert.Contains("Imported", commands.Execute($"context add \"{path}\" persistent docs").Text);
        var document = Assert.Single(engine.GetSnapshot().Context.Documents);
        Assert.Equal("pdf-pages", document.Format);
        Assert.Contains("[PDF page 1 of 2]", document.Text);
        Assert.Contains("Second page fact", document.Text);
        Assert.Equal(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))), document.SourceHash);
        var world = Path.Combine(root, "world.db");
        using (var store = new WorldStore(world)) store.Create(new(engine.GetSnapshot().WorldId, "PDF test", DateTimeOffset.UtcNow), engine.GetSnapshot());
        File.Delete(path);
        using var reopened = new WorldStore(world);
        Assert.Equal(document, Assert.Single(reopened.Load().State.Context.Documents));
        Assert.Contains("First page fact", commands.Execute($"context read {document.Id}").Text);
    }

    [Fact]
    public void MixedPdfKeepsPageNumbersAndFlagsEmptyPages()
    {
        var document = LocalContextReader.Read(Pdf("Known fact", "", "Final fact"), "docs", null);
        Assert.Contains("[PDF page 2 of 3]", document.Text);
        Assert.Contains("No extractable text", document.Text);
        Assert.Contains("Final fact", document.Text);
    }

    [Fact]
    public void ImageOnlyOrEmptyPdfDoesNotBecomeSuccessfulContext()
    {
        var error = Assert.Throws<InvalidDataException>(() => LocalContextReader.Read(Pdf(""), "docs", null));
        Assert.Contains("no extractable text", error.Message);
        Assert.Contains("OCR", error.Message);
    }

    [Fact]
    public void PdfPageLimitRejectsWithoutTruncation()
    {
        var error = Assert.Throws<InvalidDataException>(() => LocalContextReader.Read(Pdf(Enumerable.Repeat("text", 201).ToArray()), "docs", null));
        Assert.Contains("1..200", error.Message);
    }

    [Theory]
    [InlineData("fake.pdf", "not a PDF")]
    [InlineData("broken.pdf", "%PDF-1.7 invalid content")]
    [InlineData("fake.docx", "not a ZIP")]
    public void MismatchedOrCorruptDocumentsAreRejected(string name, string content)
    {
        var path = Path.Combine(root, name); File.WriteAllText(path, content);
        Assert.Throws<InvalidDataException>(() => LocalContextReader.Read(path, "docs", null));
    }

    [Fact]
    public void DocxKeepsAdjacentRunsUnicodeBreaksAndAllTableCells()
    {
        var path = Docx("<w:p><w:r><w:t>Hello </w:t></w:r><w:r><w:t>world café</w:t><w:tab/><w:t>tabbed</w:t><w:br/><w:t>next line</w:t></w:r></w:p>" +
            "<w:tbl><w:tr><w:tc><w:p><w:r><w:t>Left cell</w:t></w:r></w:p></w:tc><w:tc><w:p><w:r><w:t>Right cell</w:t></w:r></w:p></w:tc></w:tr></w:tbl>" +
            "<w:p><w:r><w:t>Final paragraph</w:t></w:r></w:p>");
        var document = LocalContextReader.Read(path, "docs", null);
        Assert.Contains("Hello world café\ttabbed\nnext line", document.Text);
        Assert.Contains("[Table row 1, cell 1]", document.Text);
        Assert.Contains("[Table row 1, cell 2]", document.Text);
        Assert.Contains("Right cell", document.Text);
        Assert.Contains("[DOCX body paragraph 4]", document.Text);
        Assert.Contains("Final paragraph", document.Text);
    }

    [Fact]
    public void EmptyDocxIsRejected()
    {
        Assert.Contains("no extractable text", Assert.Throws<InvalidDataException>(() => LocalContextReader.Read(Docx("<w:p/>"), "docs", null)).Message);
    }

    [Fact]
    public void StrictDocxNamespaceAndNestedParagraphsPreserveTextOnce()
    {
        var path = Docx("<w:p><w:r><w:t>Outer text</w:t></w:r><w:txbxContent><w:p><w:r><w:t>Inner text</w:t></w:r></w:p></w:txbxContent></w:p>");
        using (var archive = ZipFile.Open(path, ZipArchiveMode.Update))
        {
            var entry = archive.GetEntry("word/document.xml")!;
            string xml;
            using (var reader = new StreamReader(entry.Open())) xml = reader.ReadToEnd();
            entry.Delete();
            using var writer = new StreamWriter(archive.CreateEntry("word/document.xml").Open());
            writer.Write(xml.Replace("http://schemas.openxmlformats.org/wordprocessingml/2006/main", "http://purl.oclc.org/ooxml/wordprocessingml/main"));
        }
        var text = LocalContextReader.Read(path, "docs", null).Text;
        Assert.Equal(1, text.Split("Inner text").Length - 1);
        Assert.Contains("Outer text", text);
    }

    [Fact]
    public void LinkedDocxBodyIsRejectedInsteadOfSilentlyOmitted()
    {
        var path = Docx("<w:p><w:r><w:t>Partial</w:t></w:r></w:p><w:altChunk/>");
        Assert.Contains("flattened", Assert.Throws<InvalidDataException>(() => LocalContextReader.Read(path, "docs", null)).Message);
    }

    [Fact]
    public void PickerBatchWithBadPdfDoesNotCommitPartialImports()
    {
        var registry = new TerminalSessionRegistry();
        var identity = new TerminalIdentitySession(() => "batch-test"); registry.Add(identity);
        var engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis(), editAuthorizer: registry);
        identity.Attach(engine); identity.Login();
        var commands = DefaultCommands.Create(engine);
        ContextCommands.Register(commands, engine, identity); commands.UseIdentity(identity);
        var result = commands.Execute("context add -search").ContextPicker!([Pdf("valid"), Pdf("")]);
        Assert.Contains("Nothing imported", result.Text);
        Assert.Empty(engine.ContextFor(null, identity.SessionId));
    }

    [Fact]
    public void CancelledReadDoesNotStartImport()
    {
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        Assert.ThrowsAny<OperationCanceledException>(() => LocalContextReader.Read(Pdf("cancel"), "docs", null, cancellation.Token));
    }

    [Fact]
    public async Task CancellationStopsRunningWorker()
    {
        var path = Pdf(Enumerable.Repeat("cancel", 200).ToArray());
        using var cancellation = new CancellationTokenSource();
        cancellation.CancelAfter(TimeSpan.FromMilliseconds(50));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Task.Run(() => LocalContextReader.Read(path, "docs", null, cancellation.Token)));
    }

    [Fact]
    public void PasswordProtectedPdfIsRefusedWithActionableError()
    {
        var path = Path.Combine(root, "encrypted.pdf");
        // Generated blank PDF encrypted with a fixture-only password using pypdf.
        File.WriteAllBytes(path, Convert.FromBase64String("JVBERi0xLjMKJeLjz9MKMSAwIG9iago8PAovUHJvZHVjZXIgPDEyYmZiMmRiZWQ+Cj4+CmVuZG9iagoyIDAgb2JqCjw8Ci9UeXBlIC9QYWdlcwovQ291bnQgMQovS2lkcyBbIDQgMCBSIF0KPj4KZW5kb2JqCjMgMCBvYmoKPDwKL1R5cGUgL0NhdGFsb2cKL1BhZ2VzIDIgMCBSCj4+CmVuZG9iago0IDAgb2JqCjw8Ci9UeXBlIC9QYWdlCi9SZXNvdXJjZXMgPDwKPj4KL01lZGlhQm94IFsgMC4wIDAuMCA0MDAgNTAwIF0KL1BhcmVudCAyIDAgUgo+PgplbmRvYmoKNSAwIG9iago8PAovViAyCi9SIDMKL0xlbmd0aCAxMjgKL1AgNDI5NDk2NzI5MgovRmlsdGVyIC9TdGFuZGFyZAovTyA8ZDA3YWJiNGIwNzQ5YWU0YzdkYTE0ZjFlMTEzYmQ3OWFiYmE2YTk2YmQwYTcyNWI4ZWFjMGU0MzM2NmI1MjY3ZT4KL1UgPGFkMTQzNjkwMGJjYjgyY2M4Y2Q5ODA2Nzg1N2VjZWZiMjhiZjRlNWU0ZTc1OGE0MTY0MDA0ZTU2ZmZmYTAxMDg+Cj4+CmVuZG9iagp4cmVmCjAgNgowMDAwMDAwMDAwIDY1NTM1IGYgCjAwMDAwMDAwMTUgMDAwMDAgbiAKMDAwMDAwMDA1OSAwMDAwMCBuIAowMDAwMDAwMTE4IDAwMDAwIG4gCjAwMDAwMDAxNjcgMDAwMDAgbiAKMDAwMDAwMDI2MSAwMDAwMCBuIAp0cmFpbGVyCjw8Ci9TaXplIDYKL1Jvb3QgMyAwIFIKL0luZm8gMSAwIFIKL0lEIFsgPDMxMzEzNjM2NjEzNTYxMzUzODYyNjYzMDMxNjMzMzY0MzEzODM2MzIzNjYxNjI2MzMwMzk2NjM3MzQ2MjMzNjY+IDwzMTMxMzYzNjYxMzU2MTM1Mzg2MjY2MzAzMTYzMzM2NDMxMzgzNjMyMzY2MTYyNjMzMDM5NjYzNzM0NjIzMzY2PiBdCi9FbmNyeXB0IDUgMCBSCj4+CnN0YXJ0eHJlZgo0NzYKJSVFT0YK"));
        Assert.Contains("Encrypted", Assert.Throws<InvalidDataException>(() => LocalContextReader.Read(path, "docs", null)).Message);
    }
}
