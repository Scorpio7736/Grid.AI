using Grid.AI;
using System.Text.Json;

namespace Grid.Terminal;

public static class FineTuningCommands
{
    public static int Run(string[] args)
    {
        try
        {
            FineTuningJob job;
            if (args.Length == 4 && args[0] == "--finetune-prepare")
                job = FineTuningPreparation.Prepare(FineTuningPreparation.Read<FineTuningConfiguration>(args[1]),
                    FineTuningPreparation.Read<TrainingManifest>(args[2], 32 * 1024 * 1024), args[3]);
            else if (args.Length == 2 && args[0] == "--finetune-verify")
                job = FineTuningPreparation.Verify(args[1]);
            else
            {
                Console.Error.WriteLine("Usage: --finetune-prepare configuration.json dataset.json new-job-directory | --finetune-verify job-directory");
                return 2;
            }
            Console.WriteLine($"Prepared inputs verified for {job.Configuration.RunId}. SHA256 {job.Digest}");
            Console.WriteLine("State: Prepared. Training execution and runtime acceptance are recorded in separate run reports.");
            return 0;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException or ArgumentException or OverflowException)
        {
            Console.Error.WriteLine("Fine-tuning preparation failed: " + error.Message);
            return 1;
        }
    }
}
