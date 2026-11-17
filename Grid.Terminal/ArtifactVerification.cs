namespace Grid.Terminal;

public static class ArtifactVerification
{
    public static int Run()
    {
        var folder = Path.Combine(Path.GetTempPath(), "GRID-623-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var body = string.Join("\n", Enumerable.Range(1, 60).Select(i => $"Line {i:00} - GRID report template with reviewed content and recorded results."));
        foreach (var extension in new[] { ".docx", ".pdf" })
        {
            var artifact = GeneratedDocuments.Create(extension, "GRID.AI report", body);
            File.WriteAllBytes(Path.Combine(folder, "report" + extension), artifact.Bytes);
            File.WriteAllText(Path.Combine(folder, extension[1..] + "-preview.html"), artifact.PreviewHtml);
        }
        Console.WriteLine(folder);
        return 0;
    }
}
