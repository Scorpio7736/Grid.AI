using Xunit;

namespace Grid.Simulation.Tests;

public sealed class GridClockTests
{
    [Fact]
    public void NewClockIsPausedWithOneSecondTicksAndNoElapsedTime()
    {
        var clock = new GridClock();
        Assert.False(clock.IsRunning);
        Assert.Equal(TimeSpan.FromSeconds(1), clock.TickDuration);
        Assert.Equal(0, clock.TickCount);
        Assert.Equal(TimeSpan.Zero, clock.ElapsedSimulationTime);
        Assert.False(clock.TryAdvance());
        Assert.Equal(0, clock.TickCount);
        Assert.Equal(TimeSpan.Zero, clock.ElapsedSimulationTime);
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(-1L)]
    [InlineData(long.MinValue)]
    public void NonpositiveDurationIsRejected(long ticks)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new GridClock(TimeSpan.FromTicks(ticks)));
    }

    [Fact]
    public void StartAndPauseAreIdempotentAndDoNotAdvanceTime()
    {
        var clock = new GridClock();
        for (var i = 0; i < 100; i++)
        {
            clock.Start();
            clock.Start();
            Assert.True(clock.IsRunning);
            clock.Pause();
            clock.Pause();
            Assert.False(clock.IsRunning);
        }
        Assert.Equal(0, clock.TickCount);
        Assert.Equal(TimeSpan.Zero, clock.ElapsedSimulationTime);
    }

    [Theory]
    [InlineData(1L)]
    [InlineData(2500000L)]
    [InlineData(10000000L)]
    public void ExplicitAdvancementUsesExactConfiguredDuration(long durationTicks)
    {
        var clock = new GridClock(TimeSpan.FromTicks(durationTicks));
        clock.Start();
        for (var i = 0; i < 1000; i++)
            Assert.True(clock.TryAdvance());

        Assert.Equal(1000, clock.TickCount);
        Assert.Equal(TimeSpan.FromTicks(durationTicks * 1000), clock.ElapsedSimulationTime);
        Assert.Equal(TimeSpan.FromTicks(durationTicks), clock.TickDuration);
    }

    [Fact]
    public void PauseBlocksNormalTicksAndResumeContinuesExistingTime()
    {
        var clock = new GridClock();
        clock.Start();
        Assert.True(clock.TryAdvance());
        clock.Pause();
        for (var i = 0; i < 100; i++)
            Assert.False(clock.TryAdvance());
        Assert.Equal(1, clock.TickCount);
        Assert.Equal(TimeSpan.FromSeconds(1), clock.ElapsedSimulationTime);

        clock.Start();
        Assert.Equal(1, clock.TickCount);
        Assert.True(clock.TryAdvance());
        Assert.Equal(2, clock.TickCount);
        Assert.Equal(TimeSpan.FromSeconds(2), clock.ElapsedSimulationTime);
    }

    [Fact]
    public void ManualStepAdvancesOnceAndStaysPaused()
    {
        var clock = new GridClock(TimeSpan.FromMilliseconds(250));
        clock.Step();
        Assert.Equal(1, clock.TickCount);
        Assert.Equal(TimeSpan.FromMilliseconds(250), clock.ElapsedSimulationTime);
        Assert.False(clock.IsRunning);
        Assert.False(clock.TryAdvance());
        clock.Step();
        clock.Start();
        Assert.True(clock.TryAdvance());
        clock.Pause();
        clock.Step();
        Assert.Equal(4, clock.TickCount);
        Assert.Equal(TimeSpan.FromSeconds(1), clock.ElapsedSimulationTime);
        Assert.False(clock.IsRunning);
    }

    [Fact]
    public void ManualStepWhileRunningIsRejectedWithoutChangingTime()
    {
        var clock = new GridClock();
        clock.Start();
        Assert.True(clock.TryAdvance());
        Assert.Throws<InvalidOperationException>(() => clock.Step());
        Assert.True(clock.IsRunning);
        Assert.Equal(1, clock.TickCount);
        Assert.Equal(TimeSpan.FromSeconds(1), clock.ElapsedSimulationTime);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ElapsedTimeOverflowLeavesBothCountersUnchanged(bool running)
    {
        var clock = new GridClock(TimeSpan.MaxValue);
        clock.Step();
        if (running)
            clock.Start();

        Assert.Throws<OverflowException>(() =>
        {
            if (running)
                clock.TryAdvance();
            else
                clock.Step();
        });
        Assert.Equal(1, clock.TickCount);
        Assert.Equal(TimeSpan.MaxValue, clock.ElapsedSimulationTime);
        Assert.Equal(running, clock.IsRunning);
    }
}
