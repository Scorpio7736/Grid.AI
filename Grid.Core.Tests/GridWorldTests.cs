using Grid.Core;
using Xunit;

namespace Grid.Core.Tests;

public sealed class GridWorldTests
{
    private static void AssertConsistent(GridWorld world)
    {
        var memberships = world.Districts.Values.SelectMany(d => d.ProgramIds).ToArray();
        Assert.Equal(world.Programs.Count, memberships.Length);
        Assert.Equal(memberships.Length, memberships.Distinct().Count());
        foreach (var program in world.Programs.Values)
            Assert.Contains(program.Id, world.GetDistrict(program.CurrentDistrictId).ProgramIds);
        foreach (var district in world.Districts.Values)
            foreach (var id in district.ProgramIds)
                Assert.Equal(district.Id, world.GetProgram(id).CurrentDistrictId);
    }

    [Fact]
    public void FactoryCreatesFiveDistrictsAndTwentyCitizens()
    {
        var world = GridWorldFactory.CreateGenesis();
        Assert.Equal(new[] { "The Core", "Processing Sector", "Memory Banks", "Archive Sector", "Gateway" },
            world.Districts.Values.Select(d => d.Name));
        Assert.Equal(20, world.Programs.Count);
        Assert.All(world.Districts.Values, d => Assert.Equal(4, d.ProgramIds.Count));
        Assert.All(world.Programs.Values, p =>
        {
            Assert.NotEqual(Guid.Empty, p.Id);
            Assert.False(string.IsNullOrWhiteSpace(p.Name));
            Assert.False(string.IsNullOrWhiteSpace(p.Occupation));
            Assert.Equal(100f, p.Energy);
            Assert.Equal(GridProgramStatus.Idle, p.Status);
        });
        AssertConsistent(world); 
    }

    [Fact]
    public void SameSeedReproducesWorldWithoutSharingObjects()
    {
        var first = GridWorldFactory.CreateGenesis(42);
        var second = GridWorldFactory.CreateGenesis(42);
        Assert.Equal(first.Districts.Keys, second.Districts.Keys);
        Assert.Equal(first.Programs.Keys, second.Programs.Keys);
        foreach (var program in first.Programs.Values)
        {
            var copy = second.GetProgram(program.Id);
            Assert.NotSame(program, copy);
            Assert.Equal(program.CurrentDistrictId, copy.CurrentDistrictId);
        }
        Assert.NotEqual(first.Programs.Keys.First(), GridWorldFactory.CreateGenesis(43).Programs.Keys.First());
    }

    [Fact]
    public void TransferAndRepeatedSameDistrictMovePreservePopulation()
    {
        var world = GridWorldFactory.CreateGenesis();
        var citizen = world.Programs.Values.First();
        var source = world.GetDistrict(citizen.CurrentDistrictId);
        var destination = world.Districts.Values.Last();
        world.MoveProgram(citizen.Id, destination.Id);
        world.MoveProgram(citizen.Id, destination.Id);
        Assert.DoesNotContain(citizen.Id, source.ProgramIds);
        Assert.Equal(3, source.ProgramIds.Count);
        Assert.Equal(5, destination.ProgramIds.Count);
        Assert.Equal(destination.Id, citizen.CurrentDistrictId);
        Assert.Equal(20, world.Programs.Count);
        AssertConsistent(world);
        foreach (var program in world.Programs.Values)
            foreach (var district in world.Districts.Values)
                world.MoveProgram(program.Id, district.Id);
        AssertConsistent(world);
    }

    [Fact]
    public void InvalidTransfersAndLookupsDoNotChangeMembership()
    {
        var world = GridWorldFactory.CreateGenesis();
        var citizen = world.Programs.Values.First();
        var original = citizen.CurrentDistrictId;
        Assert.Throws<KeyNotFoundException>(() => world.MoveProgram(citizen.Id, Guid.Empty));
        Assert.Throws<KeyNotFoundException>(() => world.MoveProgram(Guid.Empty, original));
        Assert.Throws<KeyNotFoundException>(() => world.GetProgram(Guid.Empty));
        Assert.Throws<KeyNotFoundException>(() => world.GetDistrict(Guid.Empty));
        Assert.Equal(original, citizen.CurrentDistrictId);
        AssertConsistent(world);
    }

    [Fact]
    public void AddRejectsDuplicatesAndInvalidIds()
    {
        var world = GridWorldFactory.CreateGenesis();
        var district = world.Districts.Values.First();
        var citizen = world.Programs.Values.First();
        Assert.Throws<ArgumentNullException>(() => world.AddDistrict(null!));
        Assert.Throws<ArgumentNullException>(() => world.AddProgram(null!, district.Id));
        Assert.Throws<ArgumentException>(() => world.AddDistrict(new() { Id = Guid.Empty }));
        Assert.Throws<ArgumentException>(() => world.AddProgram(new() { Id = Guid.Empty }, district.Id));
        Assert.Throws<InvalidOperationException>(() => world.AddDistrict(district));
        Assert.Throws<InvalidOperationException>(() => world.AddDistrict(new() { Id = district.Id }));
        Assert.Throws<InvalidOperationException>(() => world.AddProgram(citizen, district.Id));
        Assert.Throws<InvalidOperationException>(() => world.AddProgram(new() { Id = citizen.Id }, district.Id));
        Assert.Equal(5, world.Districts.Count);
        Assert.Equal(20, world.Programs.Count);
        AssertConsistent(world);
    }

    [Fact]
    public void FailedAddLeavesProgramAvailableForValidAdd()
    {
        var world = GridWorldFactory.CreateGenesis();
        var citizen = new GridProgram();
        Assert.Throws<KeyNotFoundException>(() => world.AddProgram(citizen, Guid.Empty));
        Assert.Equal(Guid.Empty, citizen.CurrentDistrictId);
        Assert.DoesNotContain(citizen.Id, world.Programs.Keys);
        var district = world.Districts.Values.First();
        world.AddProgram(citizen, district.Id);
        Assert.Same(citizen, world.GetProgram(citizen.Id));
        Assert.Same(district, world.GetDistrict(district.Id));
        AssertConsistent(world);
    }

    [Fact]
    public void ObjectsCannotBeSharedBetweenWorlds()
    {
        var first = GridWorldFactory.CreateGenesis();
        var second = new GridWorld();
        var district = new GridDistrict();
        second.AddDistrict(district);
        Assert.Throws<InvalidOperationException>(() => second.AddDistrict(first.Districts.Values.First()));
        Assert.Throws<InvalidOperationException>(() => second.AddProgram(first.Programs.Values.First(), district.Id));
        Assert.Empty(second.Programs);
        Assert.Empty(district.ProgramIds);
        AssertConsistent(first);
    }

    [Fact]
    public void PublicCollectionsAndLocationCannotBypassWorldRules()
    {
        var world = GridWorldFactory.CreateGenesis();
        var district = world.Districts.Values.First();
        Assert.Throws<NotSupportedException>(() => ((IDictionary<Guid, GridProgram>)world.Programs).Clear());
        Assert.Throws<NotSupportedException>(() => ((IDictionary<Guid, GridDistrict>)world.Districts).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<Guid>)district.ProgramIds).Clear());
        Assert.False(typeof(GridProgram).GetProperty(nameof(GridProgram.CurrentDistrictId))!.SetMethod!.IsPublic);
        AssertConsistent(world);
    }
}
