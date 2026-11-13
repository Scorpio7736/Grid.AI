using Grid.AI;
using System.Text.Json;

namespace Grid.Terminal;

public static class ReleaseCommands
{
    public static async Task<int> RunAsync(string[] args)
    {
        try
        {
            if (args.Length != 3 || args[0] is not ("--release-check" or "--release-promote" or "--release-rollback"))
            {
                Console.Error.WriteLine("Usage: --release-check|--release-promote asset-root request.json | --release-rollback asset-root expected-active-id");
                return 2;
            }
            var catalog = new ModelReleaseCatalog(args[1]);
            if (args[0] == "--release-rollback")
            {
                Console.WriteLine("Release catalog rolled back to " + await catalog.RollbackAsync(args[2]));
                return 0;
            }
            var request = FineTuningPreparation.Read<ReleaseRequest>(args[2], 4 * 1024 * 1024);
            var decision = args[0] == "--release-check"
                ? await catalog.CheckAsync(request, DateTimeOffset.UtcNow)
                : await catalog.PromoteAsync(request, DateTimeOffset.UtcNow);
            Console.WriteLine(JsonSerializer.Serialize(decision));
            Console.WriteLine("Development release catalog. Live agent selection remains governed by host integration and final acceptance.");
            return decision.Passed ? 0 : 1;
        }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or JsonException or ArgumentException)
        {
            Console.Error.WriteLine("Release operation failed: " + error.Message);
            return 1;
        }
    }
}
