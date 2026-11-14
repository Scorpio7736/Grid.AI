using System.Diagnostics;
using System.Text.Json;
using Grid.Communication;
using Grid.Core;
using Grid.Persistence;
using Grid.Simulation;
using Status = Grid.Simulation.Tasks.TaskStatus;

namespace Grid.Terminal;

public static class AgenticCodingVerification
{
    public static async Task<int> RunAsync()
    {
        var folder = Path.Combine(Path.GetTempPath(), "GridAgenticCoding-" + Guid.NewGuid().ToString("N"));
        var source = Path.Combine(folder, "source");
        Directory.CreateDirectory(source);
        var evidence = new List<object>();
        var passed = false;
        try
        {
            var runtimeRoot = BundledAiRuntime.FindRoot(AppContext.BaseDirectory) ?? throw new IOException("Provision the bundled AI and SDK first.");
            var sdk = Path.Combine(runtimeRoot, "dotnet");
            var project = Path.Combine(source, "CodingProbe.csproj");
            await File.WriteAllTextAsync(project, """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup><TargetFramework>net10.0</TargetFramework><IsTestProject>true</IsTestProject><IsPackable>false</IsPackable></PropertyGroup>
                  <ItemGroup>
                    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.9.0" />
                    <PackageReference Include="xunit" Version="2.9.3" />
                    <PackageReference Include="xunit.runner.visualstudio" Version="4.0.0" />
                  </ItemGroup>
                </Project>
                """);
            await File.WriteAllTextAsync(Path.Combine(source, "NuGet.Config"), "<configuration><packageSources><clear /></packageSources></configuration>");
            await File.WriteAllTextAsync(Path.Combine(source, "Calculator.cs"), "public static class Calculator { public static int Add(int a, int b) => a - b; }\n");
            await File.WriteAllTextAsync(Path.Combine(source, "CalculatorTests.cs"), "using Xunit; public class CalculatorTests { [Fact] public void AddsTwoNumbers() { Assert.Equal(5, Calculator.Add(2, 3)); } }\n");
            var restore = new ProcessStartInfo(Path.Combine(sdk, "dotnet.exe")) { WorkingDirectory = source, UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
            foreach (var arg in new[] { "restore", project, "--packages", Path.Combine(runtimeRoot, "nuget"), "--configfile", Path.Combine(source, "NuGet.Config"), "-p:NuGetAudit=false", "--verbosity", "minimal" }) restore.ArgumentList.Add(arg);
            restore.Environment["DOTNET_ROOT"] = sdk;
            restore.Environment["DOTNET_CLI_HOME"] = Path.Combine(runtimeRoot, "home");
            restore.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
            using (var process = Process.Start(restore) ?? throw new IOException("SDK could not start."))
            {
                var stdout = process.StandardOutput.ReadToEndAsync();
                var stderr = process.StandardError.ReadToEndAsync();
                using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(2));
                try { await process.WaitForExitAsync(timeout.Token); }
                catch (OperationCanceledException) { if (!process.HasExited) process.Kill(true); throw; }
                var log = await stdout + "\n" + await stderr;
                await File.WriteAllTextAsync(Path.Combine(folder, "restore.txt"), log);
                if (process.ExitCode != 0) throw new IOException("Offline restore failed. See restore.txt.");
            }
            await using var runtime = await BundledAiRuntime.StartAsync(runtimeRoot);
            if (!runtime.Ready) throw new IOException(runtime.Status);
            using var backend = new OllamaLocalBackend(endpoint: runtime.Endpoint);
            await using var ai = new SharedLocalInference(backend);
            var registry = new TerminalSessionRegistry();
            var owner = new TerminalIdentitySession(() => "coding-verification"); registry.Add(owner);
            var engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis(), editAuthorizer: registry);
            owner.Attach(engine); owner.Login(); engine.StartAgents(owner.Reference);
            engine.ConfigureMaxisRuntime(Path.Combine(folder, "artifacts"), sdk);
            engine.RegisterDevelopmentWorkspace(owner.Reference, "demo", source, "CodingProbe.csproj");
            using var store = new WorldStore(Path.Combine(folder, "world.db"));
            var info = new WorldInfo(engine.GetSnapshot().WorldId, "Agentic coding verification", DateTimeOffset.UtcNow);
            store.Create(info, engine.GetSnapshot());
            Task Save(CancellationToken token) { store.Save(info, engine.GetSnapshot(), token); return Task.CompletedTask; }
            var commands = DefaultCommands.Create(engine);
            TaskCommands.Register(commands, engine, owner, Save);
            WorkspaceChangeCommands.Register(commands, engine, owner, ai);
            ChatCommands.Register(commands, engine, owner, ai, Save);
            commands.UseIdentity(owner);
            async Task<CommandResult> Say(string message)
            {
                Console.WriteLine("You> " + message);
                var result = commands.ExecuteUserInput(message);
                if (result.BrokerOperation is { } operation) result = await operation(default);
                evidence.Add(new { Message = message, result.Text });
                Console.WriteLine(result.Text);
                return result;
            }
            await Say("connect MCP -inline");
            async Task TestPlan()
            {
                var draft = await Say("test demo");
                if (!draft.Text.Contains("DRAFT ONLY") || !draft.Text.Contains("maxis.test")) throw new InvalidDataException("Model did not draft the requested MAXIS test task.");
                await Say("submit plan");
                await Say("approve this plan");
                await Say("run this plan");
            }
            await TestPlan();
            var initial = engine.GetSnapshot().Tasks.Records;
            if (initial.Count != 1 || initial[0].Status != Status.Failed) throw new InvalidDataException("Expected one real failing test task.");
            var logs = Directory.GetFiles(Path.Combine(folder, "artifacts"), "stdout.txt", SearchOption.AllDirectories);
            if (!logs.Any(path => File.ReadAllText(path).Contains("AddsTwoNumbers"))) throw new InvalidDataException("Test failure evidence is missing.");
            var change = await Say("update file Calculator.cs in demo with instructions Fix the public static Calculator.Add(int a, int b) method to return the sum of a and b. Preserve this API. The observed AddsTwoNumbers test expected 5 from Add(2,3) and failed. Return the complete C# file.");
            if (change.FileChange?.State != "Draft") throw new InvalidDataException("AI did not produce a reviewable code change.");
            await Say("I approve this code change");
            var applied = await Say("apply this change");
            if (applied.FileChange?.State != "Applied") throw new InvalidDataException("Approved source change was not applied.");
            await TestPlan();
            var records = engine.GetSnapshot().Tasks.Records;
            if (records.Count != 2 || records.Count(t => t.Status == Status.Completed) != 1) throw new InvalidDataException("The corrected code did not pass the actual test run.");
            var results = Directory.GetFiles(Path.Combine(folder, "artifacts"), "*.trx", SearchOption.AllDirectories)
                .Select(System.Xml.Linq.XDocument.Load).SelectMany(document => document.Descendants())
                .Where(element => element.Name.LocalName == "UnitTestResult" && (string?)element.Attribute("testName") == "CalculatorTests.AddsTwoNumbers").ToArray();
            if (!results.Any(element => (string?)element.Attribute("outcome") == "Failed") ||
                !results.Any(element => (string?)element.Attribute("outcome") == "Passed"))
                throw new InvalidDataException("Expected both failed and passed discovered-test results in TRX evidence.");
            await Save(default);
            var reloaded = store.Load().State;
            if (reloaded.WorldId != info.Id || reloaded.Tasks.Records.Count != 2) throw new InvalidDataException("Saved task evidence did not survive reload.");
            passed = true;
        }
        catch (Exception error) when (error is IOException or InvalidDataException or InvalidOperationException or ArgumentException or OperationCanceledException)
        { evidence.Add(new { Error = error.Message }); Console.Error.WriteLine(error.Message); }
        await File.WriteAllTextAsync(Path.Combine(folder, "acceptance.json"), JsonSerializer.Serialize(new { Passed = passed, Scope = "Local model plus real disposable C# test project", Evidence = evidence }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"Agentic coding acceptance: {passed}. Evidence: {folder}");
        return passed ? 0 : 1;
    }
}
