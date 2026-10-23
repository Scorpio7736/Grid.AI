using Grid.Communication;
using Grid.Core;
using Grid.Simulation;
using Grid.Terminal;
if (!args.SequenceEqual(new[]{"--local-ai"})) { Console.Error.WriteLine("Explicit opt-in required: --local-ai. Uses installed local runtime; no world file.");return 2; }
using var backend=new OllamaLocalBackend();await using var service=new SharedLocalInference(backend);
var engine=new GridSimulationEngine(GridWorldFactory.CreateGenesis());var identity=new TerminalIdentitySession(()=>"isolated-ai-probe");identity.Attach(engine);identity.Login();
var commands=DefaultCommands.Create(engine);LocalAiCommands.Register(commands,identity,service,backend);commands.UseIdentity(identity);
foreach(var command in new[]{"ai status","ai test","ai status"}) {
 var result=await commands.Execute(command).BrokerOperation!(default);Console.WriteLine(result.Text);
 if(command=="ai test" && !result.Text.Contains("LOCAL MODEL OUTPUT"))return 1;
}
return 0;
