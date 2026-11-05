using Xunit;
namespace Grid.Terminal.Tests;
public sealed class LocalFileSearchTests
{
    [Theory]
    [InlineData("hey mcp can you find the stl file on my desktop", "desktop")]
    [InlineData("find UWGB in my documents", "documents")]
    [InlineData("search my files for any STL file", "")]
    public void RecognizesLocationRequests(string input, string location)
    { Assert.Equal(location, LocalFileSearch.Parse(input)!.Location); }
    [Theory]
    [InlineData("find and explain INDIGO")]
    [InlineData("find INDIGO in my uploaded documents")]
    public void LeavesContentQuestionsToAnalysis(string input)
    { Assert.Null(LocalFileSearch.Parse(input)); }
    [Fact]
    public void FindsNamesWithoutReadingContentsAndLimitsResults()
    {
        var root = Path.Combine(Path.GetTempPath(), "GridSearch-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "UWGB"));
        try
        {
            File.WriteAllText(Path.Combine(root, "UWGB", "mesh.STL"), "private binary stand-in");
            File.WriteAllText(Path.Combine(root, "other.txt"), "STL");
            var found = LocalFileSearch.Search(root, "the stl file");
            Assert.EndsWith("mesh.STL", Assert.Single(found.Paths)); Assert.False(found.Incomplete);
            Assert.EndsWith("UWGB", Assert.Single(LocalFileSearch.Search(root, "UWGB").Paths));
            for (var i = 0; i < 60; i++) File.WriteAllText(Path.Combine(root, i + ".stl"), "");
            found = LocalFileSearch.Search(root, "any STL file");
            Assert.Equal(50, found.Paths.Count); Assert.True(found.Incomplete);
        }
        finally { Directory.Delete(root, true); }
    }
}
