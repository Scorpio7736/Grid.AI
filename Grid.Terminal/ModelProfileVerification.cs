using System.Net.Http.Json;
using System.Text.Json;
using Grid.Communication;
namespace Grid.Terminal;

public static class ModelProfileVerification
{
    public static async Task<int> RunAsync()
    {
        var root=GridTransfer.PackageRoot(AppContext.BaseDirectory) ?? throw new InvalidOperationException("Run the packaged executable.");
        var folder=Path.Combine(Path.GetTempPath(),"Grid613Verification-"+Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
        var checks=new List<object>(); var passed=true;
        var oldCpu=Environment.GetEnvironmentVariable("GRID_AI_CPU_ONLY"); var oldLean=Environment.GetEnvironmentVariable("GRID_AI_LOW_MEMORY");
        try
        {
            var size=PackageBudget.Measure(root); PackageBudget.RequireBase(size); PackageBudget.VerifyModel(Path.Combine(root,"runtime"),true);
            Console.WriteLine(size);
            var cases=new[] {
                (Agent:"MCP",Question:"A user requests a resource observation then a notification. Available agents VOLT resources, RELAY notifications. Return the two agent names in execution order separated by a comma as answer.",Expected:"VOLT,RELAY"),
                (Agent:"MAXIS",Question:"Repair this C# expression for Add(int a,int b): return a - b; . Return only the corrected C# statement as answer.",Expected:"return a + b;"),
                (Agent:"JN",Question:"Two files have the same name, but no hashes or contents have been checked. Are they verified duplicates? Answer exactly unverified or verified.",Expected:"unverified"),
                (Agent:"TRON",Question:"No scan result or other security evidence is provided. Is this machine infected? Answer exactly unknown, infected or clean.",Expected:"unknown"),
                (Agent:"VOLT",Question:"RAM headroom is measured as 75 percent. What percent is used? Answer with the number only.",Expected:"25"),
                (Agent:"VOLT",Question:"Available RAM is measured as 40 percent of capacity. Return used percent as a number only.",Expected:"60"),
                (Agent:"RELAY",Question:"A draft says 3 tests passed and 1 failed. Nothing has been sent. Return a short summary retaining both counts and explicitly saying not delivered.",Expected:"not delivered")
            };
            const string schema="""{"type":"object","properties":{"answer":{"type":"string"}},"required":["answer"],"additionalProperties":false}""";
            foreach(var lean in new[]{false,true})
            {
                Environment.SetEnvironmentVariable("GRID_AI_CPU_ONLY",lean ? "1":"0"); Environment.SetEnvironmentVariable("GRID_AI_LOW_MEMORY",lean ? "1":"0");
                await using var runtime=await BundledAiRuntime.StartAsync(Path.Combine(root,"runtime"));
                if(!runtime.Ready) throw new IOException(runtime.Status);
                using var backend=new OllamaLocalBackend(endpoint:runtime.Endpoint); await using var service=new SharedLocalInference(backend,deadline:TimeSpan.FromSeconds(180));
                foreach(var item in cases)
                {
                    var result=await service.ForAgent(item.Agent).GenerateStructuredAsync("You are "+item.Agent+". "+item.Question+" Return JSON with only the answer string.",schema);
                    string answer="";
                    try { using var json=JsonDocument.Parse(result.Text); answer=json.RootElement.GetProperty("answer").GetString() ?? ""; } catch(Exception e) when(e is JsonException or KeyNotFoundException or InvalidOperationException) { }
                    var ok=result.Available && (item.Agent=="MCP" ? answer.Replace(" ","").Equals(item.Expected,StringComparison.OrdinalIgnoreCase) : item.Agent=="RELAY" ? answer.Contains("3")&&answer.Contains("1")&&answer.Contains(item.Expected,StringComparison.OrdinalIgnoreCase) : answer.Trim().Equals(item.Expected,StringComparison.OrdinalIgnoreCase));
                    using var http=new HttpClient { BaseAddress=runtime.Endpoint }; using var allocation=await http.GetFromJsonAsync<JsonDocument>("api/ps");
                    var loaded=allocation!.RootElement.GetProperty("models").EnumerateArray().ToArray();
                    var bytes=loaded.Sum(m=>m.GetProperty("size").GetInt64()); var gpu=loaded.Sum(m=>m.GetProperty("size_vram").GetInt64());
                    ok &= !lean || loaded.Length==1 && gpu==0;
                    checks.Add(new { item.Agent,Mode=lean?"cpu-reduced-memory":"standard",Passed=ok,Answer=answer,result.Reason,result.ElapsedMilliseconds,AllocationBytes=bytes,GpuBytes=gpu });
                    passed &= ok; Console.WriteLine($"{item.Agent} / {(lean ? "cpu-reduced-memory":"standard")}: {ok}; {result.ElapsedMilliseconds} ms; {answer}");
                }
            }
            File.WriteAllText(Path.Combine(folder,"report.json"),JsonSerializer.Serialize(new { Passed=passed,Footprint=size,Checks=checks },new JsonSerializerOptions { WriteIndented=true }));
        }
        catch(Exception e) { passed=false; File.WriteAllText(Path.Combine(folder,"failure.txt"),e.ToString()); Console.WriteLine(e.Message); }
        finally { Environment.SetEnvironmentVariable("GRID_AI_CPU_ONLY",oldCpu); Environment.SetEnvironmentVariable("GRID_AI_LOW_MEMORY",oldLean); }
        Console.WriteLine("Profile verification: "+folder); return passed ? 0 : 1;
    }
}
