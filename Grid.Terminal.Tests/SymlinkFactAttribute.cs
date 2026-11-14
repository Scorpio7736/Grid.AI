using Xunit;

namespace Grid.Terminal.Tests;

/// <summary>Report link-fixture capability separately from the runtime policy being tested.</summary>
public sealed class SymlinkFactAttribute : FactAttribute
{
    private static readonly Lazy<string?> MissingCapability = new(Probe);
    public SymlinkFactAttribute() => Skip = Environment.GetEnvironmentVariable("GRID_TEST_DISABLE_SYMLINKS") == "1"
        ? "Symbolic-link fixture disabled for explicit capability simulation; reparse protection remains unverified in this run."
        : MissingCapability.Value;

    private static string? Probe()
    {
        var root = Path.Combine(Path.GetTempPath(), "GridLinkCapability-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var file = Path.Combine(root, "target.txt"); var fileLink = Path.Combine(root, "file-link"); var directoryLink = Path.Combine(root, "directory-link");
        try
        {
            File.WriteAllText(file, "probe"); File.CreateSymbolicLink(fileLink, file);
            Directory.CreateSymbolicLink(directoryLink, root);
            return null;
        }
        catch (IOException error) when (OperatingSystem.IsWindows() && (error.HResult & 0xffff) == 1314)
        {
            return "Windows account cannot create symbolic links (ERROR_PRIVILEGE_NOT_HELD). This fixture requires a symlink-capable test environment; ordinary GRID use does not. Reparse protection is not verified by this skipped case.";
        }
        finally
        {
            if (File.Exists(fileLink)) File.Delete(fileLink);
            if (Directory.Exists(directoryLink)) Directory.Delete(directoryLink);
            if (File.Exists(file)) File.Delete(file);
            Directory.Delete(root);
        }
    }
}
