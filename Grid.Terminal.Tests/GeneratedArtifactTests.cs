using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using Grid.Agent;
using UglyToad.PdfPig;
using Xunit;

namespace Grid.Terminal.Tests;

public sealed class GeneratedArtifactTests
{
    [Theory]
    [InlineData(".pdf")]
    [InlineData(".docx")]
    public void ReportsRetainWrappedContentAndEscapePreview(string extension)
    {
        var body = "<script>alert('x')</script>\n" + string.Join("\n", Enumerable.Range(0, 60).Select(i => "ROW" + i));
        var artifact = GeneratedDocuments.Create(extension, "Review", body);
        Assert.DoesNotContain("<script>", artifact.PreviewHtml);
        Assert.Contains("&lt;script&gt;", artifact.PreviewHtml);
        if (extension == ".pdf")
        {
            using var pdf = PdfDocument.Open(artifact.Bytes);
            Assert.Equal(2, pdf.NumberOfPages);
            Assert.Contains("ROW59", string.Concat(pdf.GetPages().Select(p => p.Text)));
        }
        else
        {
            using var zip = new ZipArchive(new MemoryStream(artifact.Bytes));
            using var input = zip.GetEntry("word/document.xml")!.Open();
            var doc = XDocument.Load(input);
            Assert.Contains("ROW59", doc.Root!.Value);
            Assert.Single(doc.Descendants(), e => e.Name.LocalName == "pageBreakBefore");
            Assert.Equal(artifact.Bytes, GeneratedDocuments.Create(extension, "Review", body).Bytes);
        }
    }

    [Fact]
    public void TemplateRefusesUnsupportedContentAndFormats()
    {
        Assert.Throws<ArgumentException>(() => GeneratedDocuments.Create(".pdf", "Title", "Unicode \u4e00"));
        Assert.Throws<ArgumentException>(() => GeneratedDocuments.Create(".exe", "Title", "Body"));
        Assert.Throws<ArgumentException>(() => GeneratedDocuments.Create(".docx", "Title", new string('x', 10001)));
    }

    [Fact]
    public void PngValidationDecodesPixelsAndRejectsCorruptionOrExcessData()
    {
        var png = Png([0, 255, 0, 0, 255]);
        Assert.Equal(png, GeneratedImageValidation.ValidatePng(png).Bytes);
        var corrupt = png.ToArray(); corrupt[corrupt.Length - 1] ^= 1;
        Assert.Throws<InvalidDataException>(() => GeneratedImageValidation.ValidatePng(corrupt));
        Assert.Throws<InvalidDataException>(() => GeneratedImageValidation.ValidatePng(Png([5, 255, 0, 0, 255])));
        Assert.Throws<InvalidDataException>(() => GeneratedImageValidation.ValidatePng(Png([0, 255, 0, 0, 255, 1])));
        Assert.Throws<InvalidDataException>(() => GeneratedImageValidation.ValidatePng(png.Concat(new byte[] { 0 }).ToArray()));
    }

    [Theory]
    [InlineData("MCP", 1)]
    [InlineData("MAXIS", 2)]
    [InlineData("unknown", 1)]
    public void DelegationRejectsRecursiveOrUnknownTargets(string child, int depth)
    {
        Assert.Throws<ArgumentException>(() => ToolDelegationPolicy.Validate("MCP", [child], depth));
    }

    private static byte[] Png(byte[] pixels)
    {
        using var output = new MemoryStream();
        output.Write(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
        var header = new byte[13];
        BinaryPrimitives.WriteUInt32BigEndian(header, 1);
        BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(4), 1);
        header[8] = 8; header[9] = 6;
        Chunk(output, "IHDR", header);
        using var encoded = new MemoryStream();
        using (var zip = new ZLibStream(encoded, CompressionMode.Compress, true)) zip.Write(pixels);
        Chunk(output, "IDAT", encoded.ToArray());
        Chunk(output, "IEND", []);
        return output.ToArray();
    }

    private static void Chunk(Stream output, string name, byte[] bytes)
    {
        var length = new byte[4]; BinaryPrimitives.WriteUInt32BigEndian(length, (uint)bytes.Length); output.Write(length);
        var payload = Encoding.ASCII.GetBytes(name).Concat(bytes).ToArray(); output.Write(payload);
        uint crc = uint.MaxValue;
        foreach (var value in payload)
        {
            crc ^= value;
            for (var bit = 0; bit < 8; bit++) crc = (crc >> 1) ^ ((crc & 1) == 0 ? 0U : 0xedb88320U);
        }
        BinaryPrimitives.WriteUInt32BigEndian(length, ~crc); output.Write(length);
    }
}
