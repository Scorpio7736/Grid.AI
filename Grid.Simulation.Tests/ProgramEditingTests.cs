using Grid.Core;
using Grid.Simulation.Editing;
using Grid.TestSupport;
using Xunit;

namespace Grid.Simulation.Tests;

public sealed class ProgramEditingTests
{
    private sealed class Fixture
    {
        public GridWorld World { get; }
        public GridProgram Program => World.Programs.Values.First();
        public TestEditAuthority Authority { get; }
        public GridSimulationEngine Engine { get; }
        public Fixture(TimeProvider? time = null, bool working = false)
        {
            World = TestEdit.Genesis(p => { if (working) p.Status = GridProgramStatus.Working; });
            Authority = new(World.InstanceId);
            Engine = new(World, timeProvider: time, editAuthorizer: Authority);
        }
        public ProgramEditPreview Preview(params ProgramFieldEdit[] fields)
        {
            var result = Engine.PrepareEdit(Authority.Session, new(Program.Id, fields));
            Assert.Equal(ProgramEditOutcome.Prepared, result.Outcome);
            return result.Preview!;
        }
        public ProgramEditResult Apply(params ProgramFieldEdit[] fields) => Engine.ApplyEdit(Authority.Session, Preview(fields));
    }
    private static ProgramFieldEdit Name(string name) => new("name", new TextValue(name));
    private static ProgramFieldEdit Energy(float energy) => new("energy", new EnergyValue(energy));

    [Fact]
    public void PreviewIsReadOnlyAndAtomicApplyReturnsAttributedActualValues()
    {
        var f = new Fixture();
        var originalId = f.Program.Id;
        var district = f.World.Districts.Values.Last();
        var preview = f.Preview(Name("  Renamed  "), new("occupation", new TextValue(" Operator ")),
            Energy(75.5f), new("status", new StatusValue(GridProgramStatus.Recharging)),
            new("district", new DistrictValue(district.Id)));
        Assert.Equal("Citizen 001", f.Program.Name);
        Assert.Equal(0, f.Program.Revision);
        Assert.Throws<NotSupportedException>(() => ((IList<ProgramFieldChange>)preview.Changes).Clear());
        var result = f.Engine.ApplyEdit(f.Authority.Session, preview);
        Assert.Equal(ProgramEditOutcome.Applied, result.Outcome);
        Assert.Equal("Renamed", f.Program.Name);
        Assert.Equal("Operator", f.Program.Occupation);
        Assert.Equal(75.5f, f.Program.Energy);
        Assert.Equal(GridProgramStatus.Recharging, f.Program.Status);
        Assert.Equal(district.Id, f.Program.CurrentDistrictId);
        Assert.Equal(originalId, f.Program.Id);
        Assert.Equal(1, f.Program.Revision); // All five fields are one transaction.
        Assert.Equal(20, f.World.Districts.Values.Sum(d => d.ProgramIds.Count));
        Assert.Single(f.World.Districts.Values, d => d.ProgramIds.Contains(originalId));
        Assert.Equal(f.Authority.Identity, result.Receipt!.Identity);
        Assert.Equal(0, result.Receipt.BeforeRevision);
        Assert.Equal(1, result.Receipt.Revision);
        Assert.Equal(5, result.Receipt.Changes.Count);
        Assert.Equal(new TextValue("Citizen 001"), result.Receipt.Changes[0].Before);
        Assert.Equal(new TextValue("Renamed"), result.Receipt.Changes[0].After);
        Assert.Equal(0, f.Engine.TickCount);
        Assert.Null(f.Engine.LastTick);
        Assert.Equal(ProgramEditOutcome.Conflict, f.Engine.ApplyEdit(f.Authority.Session, preview).Outcome);
    }

    [Theory]
    [InlineData("", false)]
    [InlineData("  ", false)]
    [InlineData("line\nbreak", false)]
    [InlineData("\tname", false)]
    [InlineData("bad\u001b", false)]
    [InlineData("  Name With Spaces  ", true)]
    [InlineData("Житель", true)]
    public void TextRules(string text, bool accepted)
    {
        var f = new Fixture();
        var result = f.Engine.PrepareEdit(f.Authority.Session, new(f.Program.Id, new[] { Name(text) }));
        Assert.Equal(accepted ? ProgramEditOutcome.Prepared : ProgramEditOutcome.ValidationFailed, result.Outcome);
        Assert.Equal(0, f.Program.Revision);
    }

    [Theory]
    [InlineData("name", 80, true)]
    [InlineData("name", 81, false)]
    [InlineData("occupation", 120, true)]
    [InlineData("occupation", 121, false)]
    public void TextBoundaries(string key, int length, bool accepted)
    {
        var f = new Fixture();
        var result = f.Engine.PrepareEdit(f.Authority.Session,
            new(f.Program.Id, new[] { new ProgramFieldEdit(key, new TextValue(new string('x', length))) }));
        Assert.Equal(accepted ? ProgramEditOutcome.Prepared : ProgramEditOutcome.ValidationFailed, result.Outcome);
    }

    [Theory]
    [InlineData("0", true)]
    [InlineData("75.5", true)]
    [InlineData("100", true)]
    [InlineData("+.5", true)]
    [InlineData("-1", false)]
    [InlineData("100.01", false)]
    [InlineData("100.00000000000000000000000000001", false)]
    [InlineData("-0.00000000000000000000000000001", false)]
    [InlineData("NaN", false)]
    [InlineData("Infinity", false)]
    [InlineData("1,000", false)]
    [InlineData("1e1", false)]
    [InlineData("1\n2", false)]
    public void EnergyParserChecksExactBoundsBeforeFloatRounding(string text, bool accepted)
    {
        Assert.Equal(accepted, ProgramEditFieldRegistry.CreateDefault().Parse("ENERGY", text).Success);
    }

    [Theory]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(-1f)]
    [InlineData(101f)]
    public void InvalidTypedEnergyNeverPartiallyRenames(float value)
    {
        var f = new Fixture();
        var result = f.Engine.PrepareEdit(f.Authority.Session,
            new(f.Program.Id, new[] { Name("Changed"), Energy(value) }));
        Assert.Equal(ProgramEditOutcome.ValidationFailed, result.Outcome);
        Assert.Null(result.Preview);
        Assert.Equal("Citizen 001", f.Program.Name);
        Assert.Equal(0, f.Program.Revision);
    }

    [Fact]
    public void NamedStatusesAndTypedValuesAreValidated()
    {
        var registry = ProgramEditFieldRegistry.CreateDefault();
        Assert.Equal(new StatusValue(GridProgramStatus.Recharging), registry.Parse("status", "recharging").Edit!.Value);
        Assert.False(registry.Parse("status", "1").Success);
        Assert.False(registry.Parse("status", "Idle,Working").Success);
        var f = new Fixture();
        foreach (var value in new ProgramEditValue[] { new StatusValue((GridProgramStatus)99), new TextValue("Idle") })
            Assert.Equal(ProgramEditOutcome.ValidationFailed, f.Engine.PrepareEdit(f.Authority.Session,
                new(f.Program.Id, new[] { new ProgramFieldEdit("status", value) })).Outcome);
    }

    [Fact]
    public void UnknownDuplicateEmptyAndInvalidDistrictRequestsDoNotMutate()
    {
        var f = new Fixture();
        ProgramFieldEdit[][] invalid =
        [
            [], [new("id", new DistrictValue(Guid.NewGuid()))],
            [Name("First"), new("NAME", new TextValue("Second"))],
            [new("district", new DistrictValue(Guid.Empty))],
            [new("district", new DistrictValue(Guid.NewGuid()))],
            [Name("Changed"), new("district", new DistrictValue(Guid.NewGuid()))],
            [new("name", new TextValue(null!))], [null!]
        ];
        foreach (var fields in invalid)
            Assert.Equal(ProgramEditOutcome.ValidationFailed,
                f.Engine.PrepareEdit(f.Authority.Session, new(f.Program.Id, fields)).Outcome);
        Assert.Equal(0, f.Program.Revision);
        Assert.Equal(20, f.World.Districts.Values.Sum(d => d.ProgramIds.Count));
    }

    [Fact]
    public void DefaultProviderAndForgedSessionDenyBeforeTargetLookup()
    {
        var f = new Fixture();
        var unconfigured = new GridSimulationEngine(f.World);
        foreach (var id in new[] { f.Program.Id, Guid.NewGuid() })
        {
            var denied = unconfigured.PrepareEdit(f.Authority.Session, new(id, new[] { Name("X") }));
            Assert.Equal(ProgramEditOutcome.Forbidden, denied.Outcome);
            Assert.Null(denied.Preview);
            Assert.Null(denied.Errors);
        }
        Assert.Equal(ProgramEditOutcome.Forbidden, f.Engine.PrepareEdit(new(Guid.NewGuid(), 0),
            new(f.Program.Id, new[] { Name("X") })).Outcome);
        Assert.Equal(ProgramEditOutcome.NotFound, f.Engine.PrepareEdit(f.Authority.Session,
            new(Guid.NewGuid(), new[] { Name("X") })).Outcome);
    }

    [Fact]
    public void AgentSwitchRevocationAndOldGenerationsCannotApply()
    {
        var f = new Fixture();
        var session = f.Authority.Session;
        var preview = f.Preview(Name("X"));
        f.Authority.Switch(false);
        Assert.Equal(ProgramEditOutcome.Forbidden, f.Engine.ApplyEdit(session, preview).Outcome);
        Assert.Equal(ProgramEditOutcome.Forbidden, f.Engine.PrepareEdit(f.Authority.Session,
            new(f.Program.Id, new[] { Name("X") })).Outcome);
        f.Authority.Switch(true);
        Assert.Equal(ProgramEditOutcome.Conflict, f.Engine.ApplyEdit(f.Authority.Session, preview).Outcome);
        var newPreview = f.Preview(Name("New"));
        f.Authority.Revoke();
        Assert.Equal(ProgramEditOutcome.Forbidden, f.Engine.ApplyEdit(f.Authority.Session, newPreview).Outcome);
        Assert.Equal(0, f.Program.Revision);
    }

    [Fact]
    public void PreviewCannotCrossWorldOrEngineInstance()
    {
        var f = new Fixture();
        var preview = f.Preview(Name("X"));
        var second = new GridSimulationEngine(f.World, editAuthorizer: f.Authority);
        Assert.Equal(ProgramEditOutcome.Conflict, second.ApplyEdit(f.Authority.Session, preview).Outcome);
        var other = new Fixture();
        Assert.Equal(ProgramEditOutcome.Conflict, other.Engine.ApplyEdit(other.Authority.Session, preview).Outcome);
    }

    [Fact]
    public void TickAndMoveInvalidatePreviewEvenIfStateReturns()
    {
        var f = new Fixture(working: true);
        var beforeTick = f.Preview(Name("X"));
        f.Engine.Step();
        Assert.Equal(ProgramEditOutcome.Conflict, f.Engine.ApplyEdit(f.Authority.Session, beforeTick).Outcome);
        var beforeMove = f.Preview(Name("Y"));
        var originalDistrict = f.Program.CurrentDistrictId;
        f.World.MoveProgram(f.Program.Id, f.World.Districts.Values.Last().Id);
        f.World.MoveProgram(f.Program.Id, originalDistrict);
        Assert.Equal(ProgramEditOutcome.Conflict, f.Engine.ApplyEdit(f.Authority.Session, beforeMove).Outcome);
        Assert.Equal(3, f.Program.Revision);
    }

    [Fact]
    public void UnrelatedChangesAndUnchangedTicksDoNotConflict()
    {
        var f = new Fixture();
        var preview = f.Preview(Name("X"));
        f.Engine.Step();
        var other = f.World.Programs.Values.Last();
        f.World.MoveProgram(other.Id, f.World.Districts.Values.First().Id);
        Assert.Equal(ProgramEditOutcome.Applied, f.Engine.ApplyEdit(f.Authority.Session, preview).Outcome);
        Assert.Equal(1, f.Engine.TickCount);
        Assert.NotNull(f.Engine.LastTick);
    }

    [Fact]
    public void NoOpAndRenameNeverGrantPrivileges()
    {
        var f = new Fixture();
        var result = f.Apply(Name(f.Program.Name), Energy(100),
            new("district", new DistrictValue(f.Program.CurrentDistrictId)));
        Assert.Equal(ProgramEditOutcome.NoChange, result.Outcome);
        Assert.Equal(0, f.Program.Revision);
        Assert.All(result.Receipt!.Changes, c => Assert.False(c.IsChanged));
        f.Apply(Name("Master Control"));
        f.Authority.Switch(false);
        Assert.Equal(ProgramEditOutcome.Forbidden, f.Engine.PrepareEdit(f.Authority.Session,
            new(f.Program.Id, new[] { Energy(50) })).Outcome);
    }

    [Fact]
    public void OneTimeEditIsFollowedByNormalSimulationAndPreservesLastTick()
    {
        var f = new Fixture(working: true);
        var last = f.Engine.Step();
        Assert.Equal(ProgramEditOutcome.Applied, f.Apply(Energy(75)).Outcome);
        Assert.Same(last, f.Engine.LastTick);
        Assert.Equal(1, f.Engine.TickCount);
        f.Engine.Step();
        Assert.Equal(74f, f.Program.Energy);
        Assert.Equal(3, f.Program.Revision);
    }

    [Fact]
    public void CancelledRequestsAndConsumedPreviewsDoNotChangeState()
    {
        var f = new Fixture();
        var preview = f.Preview(Name("X"));
        var cancelled = new CancellationToken(true);
        Assert.Equal(ProgramEditOutcome.Cancelled, f.Engine.ApplyEdit(f.Authority.Session, preview, cancelled).Outcome);
        Assert.Equal(ProgramEditOutcome.Cancelled, f.Engine.PrepareEdit(f.Authority.Session,
            new(f.Program.Id, new[] { Name("X") }), cancelled).Outcome);
        Assert.Equal(0, f.Program.Revision);
        Assert.Equal(ProgramEditOutcome.Applied, f.Engine.ApplyEdit(f.Authority.Session, preview).Outcome);
        Assert.Equal(ProgramEditOutcome.Conflict, f.Engine.ApplyEdit(f.Authority.Session, preview).Outcome);
    }

    [Fact]
    public void InitializationWorksButOwnedProgramSettersAreBlocked()
    {
        var program = new GridProgram { Name = "Initial", Energy = 50, Occupation = "Technician", Status = GridProgramStatus.Working };
        var world = new GridWorld();
        var district = new GridDistrict();
        world.AddDistrict(district);
        world.AddProgram(program, district.Id);
        Assert.Throws<InvalidOperationException>(() => program.Name = "Bypass");
        Assert.Throws<InvalidOperationException>(() => program.Occupation = "Bypass");
        Assert.Throws<InvalidOperationException>(() => program.Energy = 10);
        Assert.Throws<InvalidOperationException>(() => program.Status = GridProgramStatus.Offline);
        Assert.Equal(0, program.Revision);
    }

    [Fact]
    public void RegistrySupportsTypedExtensionsAndRejectsDuplicateKeys()
    {
        var defaults = ProgramEditFieldRegistry.CreateDefault();
        var descriptor = defaults.Fields.Single(f => f.Key == "occupation") with { Key = "job-label" };
        var registry = new ProgramEditFieldRegistry(defaults.Fields.Append(descriptor));
        Assert.True(registry.Parse("JOB-LABEL", "Engineer").Success);
        Assert.Throws<ArgumentException>(() => new ProgramEditFieldRegistry(defaults.Fields.Append(descriptor with { Key = "NAME" })));
        var world = GridWorldFactory.CreateGenesis();
        var authority = new TestEditAuthority(world.InstanceId);
        var engine = new GridSimulationEngine(world, editAuthorizer: authority, editFields: registry);
        var preview = engine.PrepareEdit(authority.Session, new(world.Programs.Keys.First(),
            new[] { registry.Parse("job-label", "Engineer").Edit! }));
        Assert.Equal(ProgramEditOutcome.Applied, engine.ApplyEdit(authority.Session, preview.Preview!).Outcome);
        Assert.Equal("Engineer", world.Programs.Values.First().Occupation);
    }

    [Fact]
    public void PreviewCopiesRequestAndRoundTripsStoredFloat()
    {
        var f = new Fixture();
        var parsed = f.Engine.EditableFields.Parse("energy", "0.123456789");
        var fields = new[] { parsed.Edit! };
        var preview = f.Engine.PrepareEdit(f.Authority.Session, new(f.Program.Id, fields)).Preview!;
        fields[0] = Energy(1);
        f.Engine.ApplyEdit(f.Authority.Session, preview);
        Assert.Equal(((EnergyValue)parsed.Edit!.Value).Value, f.Program.Energy);
        var displayed = ProgramEditFormatting.Format(preview.Changes.Single().After);
        Assert.Equal(parsed.Edit.Value, f.Engine.EditableFields.Parse("energy", displayed).Edit!.Value);
    }

    [Fact]
    public async Task ConcurrentEditsAndTicksKeepRevisionsAndMembershipConsistent()
    {
        var f = new Fixture(working: true);
        f.Engine.Start();
        var ticks = Task.Run(() => { for (var i = 0; i < 100; i++) f.Engine.Tick(); });
        var edits = Task.Run(() =>
        {
            for (var i = 0; i < 100; i++)
            {
                var preview = f.Preview(Name($"Name {i}"));
                var result = f.Engine.ApplyEdit(f.Authority.Session, preview);
                Assert.True(result.Outcome is ProgramEditOutcome.Applied or ProgramEditOutcome.Conflict);
            }
        });
        await Task.WhenAll(ticks, edits).WaitAsync(TimeSpan.FromSeconds(5));
        var snapshot = f.Engine.GetSnapshot();
        Assert.Equal(100, snapshot.TickCount);
        Assert.Equal(0f, snapshot.Programs.Single(p => p.Id == f.Program.Id).Energy);
        Assert.Equal(20, snapshot.Districts.Sum(d => d.ProgramIds.Count));
        Assert.True(f.Program.Revision >= 100);
    }

    [Fact]
    public async Task RevocationWaitsForAdmittedCommitThenDeniesLaterEdits()
    {
        var time = new BlockingTime();
        var f = new Fixture(time);
        var preview = f.Preview(Name("Committed"));
        var apply = Task.Run(() => f.Engine.ApplyEdit(f.Authority.Session, preview));
        await time.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var attempted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var revoke = Task.Run(() => { attempted.SetResult(); f.Authority.Revoke(); });
        try
        {
            await attempted.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.False(revoke.IsCompleted);
        }
        finally { time.Release.Set(); }
        Assert.Equal(ProgramEditOutcome.Applied, (await apply.WaitAsync(TimeSpan.FromSeconds(5))).Outcome);
        await revoke.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(ProgramEditOutcome.Forbidden, f.Engine.PrepareEdit(f.Authority.Session,
            new(f.Program.Id, new[] { Name("Denied") })).Outcome);
    }

    [Theory]
    [InlineData(float.Epsilon)]
    [InlineData(0.0000001f)]
    [InlineData(99.99999f)]
    public void EnergyFormattingCanBeParsedWithoutLosingStoredPrecision(float value)
    {
        var text = ProgramEditFormatting.Format(new EnergyValue(value));
        var parsed = ProgramEditFieldRegistry.CreateDefault().Parse("energy", text);
        Assert.True(parsed.Success);
        Assert.Equal(new EnergyValue(value), parsed.Edit!.Value);
    }

    [Fact]
    public void CancellationDuringPreviewValidationReturnsNoPreview()
    {
        using var cancellation = new CancellationTokenSource();
        var defaults = ProgramEditFieldRegistry.CreateDefault();
        var name = defaults.Fields.Single(f => f.Key == "name");
        var replacement = name with { Normalize = value => { cancellation.Cancel(); return name.Normalize(value); } };
        var registry = new ProgramEditFieldRegistry(defaults.Fields.Where(f => f.Key != "name").Append(replacement));
        var world = GridWorldFactory.CreateGenesis();
        var authority = new TestEditAuthority(world.InstanceId);
        var engine = new GridSimulationEngine(world, editAuthorizer: authority, editFields: registry);
        var result = engine.PrepareEdit(authority.Session,
            new(world.Programs.Keys.First(), new[] { Name("X") }), cancellation.Token);
        Assert.Equal(ProgramEditOutcome.Cancelled, result.Outcome);
        Assert.Null(result.Preview);
        Assert.Equal(0, world.Programs.Values.First().Revision);
    }
    private sealed class BlockingTime : TimeProvider
    {
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public ManualResetEventSlim Release { get; } = new();
        public override DateTimeOffset GetUtcNow()
        {
            Entered.TrySetResult();
            if (!Release.Wait(TimeSpan.FromSeconds(5))) throw new TimeoutException("Test commit was not released.");
            return DateTimeOffset.UnixEpoch;
        }
    }
}
