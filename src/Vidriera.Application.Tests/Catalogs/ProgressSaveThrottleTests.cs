using Vidriera.Application.Catalogs;

namespace Vidriera.Application.Tests.Catalogs;

public class ProgressSaveThrottleTests
{
    private static readonly DateTime T0 = new(2026, 11, 11, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void SavesFirstUpdate_ThenSkipsUntilIntervalPasses()
    {
        var throttle = new ProgressSaveThrottle(TimeSpan.FromSeconds(1));

        Assert.True(throttle.ShouldSave(new("rasterizing", 1, 100), T0));
        Assert.False(throttle.ShouldSave(new("rasterizing", 2, 100), T0.AddMilliseconds(300)));
        Assert.False(throttle.ShouldSave(new("rasterizing", 3, 100), T0.AddMilliseconds(900)));
        Assert.True(throttle.ShouldSave(new("rasterizing", 4, 100), T0.AddMilliseconds(1000)));
    }

    [Fact]
    public void AlwaysSavesStageChanges()
    {
        var throttle = new ProgressSaveThrottle(TimeSpan.FromSeconds(1));

        Assert.True(throttle.ShouldSave(new("rasterizing", 1, 100), T0));
        Assert.True(throttle.ShouldSave(new("finalizing", 1, 3), T0.AddMilliseconds(10)));
    }

    [Fact]
    public void AlwaysSavesTheLastStepOfAStage()
    {
        var throttle = new ProgressSaveThrottle(TimeSpan.FromSeconds(1));

        Assert.True(throttle.ShouldSave(new("rasterizing", 99, 100), T0));
        Assert.True(throttle.ShouldSave(new("rasterizing", 100, 100), T0.AddMilliseconds(10)));
    }
}
