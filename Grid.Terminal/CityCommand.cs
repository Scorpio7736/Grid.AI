using Grid.Simulation;

namespace Grid.Terminal;

public static class CityCommand
{
    public static void Register(CommandDispatcher commands, GridSimulationEngine engine) =>
        commands.Register(new("city", "city", "Inspect the saved city layout and creation hardware coverage.", args =>
        {
            if (args.Count != 0) return new("Usage: city");
            var blueprint = engine.GetSnapshot().City;
            if (blueprint is null) return new("Legacy/demo world: no generated city layout. Existing world preserved.");
            var city = blueprint.Read();
            var lines = new List<string> { $"Generator: {city.GeneratorVersion} | Seed: {city.Seed}",
                $"Saved layout: {city.Districts.Count} districts, {city.Roads.Count} streets, {city.Districts.Sum(d => d.Lots.Count)} building lots.",
                "Semantic geometry in metres; 3D rendering and application residents are not implemented." };
            foreach (var category in city.Profile.Categories)
                lines.Add($"{category.Kind}: {category.Status} | Components: {category.Components.Count} | Truncated: {category.Truncated}" +
                    $" | Capacity fields: {category.Components.Count(c => c.CapacityBytes.HasValue)} | Unit fields: {category.Components.Count(c => c.Units.HasValue)}");
            foreach (var d in city.Districts)
                lines.Add($"{d.Name} ({d.Id}) | Centre ({d.X},{d.Z}) | {d.Width}x{d.Depth} m | Lots: {d.Lots.Count}");
            return new(string.Join(Environment.NewLine, lines));
        }));
}
