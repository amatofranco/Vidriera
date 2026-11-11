using Vidriera.Application.Catalogs;

namespace Vidriera.Application.Tests.Catalogs;

public class CatalogGenerationGateTests
{
    [Fact]
    public async Task HasWaiters_IsTrueOnlyWhileSomeoneIsWaitingToEnter()
    {
        var gate = new CatalogGenerationGate();
        Assert.True(await gate.TryEnterAsync(TimeSpan.Zero, CancellationToken.None));
        Assert.False(gate.HasWaiters);

        var waiter = gate.TryEnterAsync(TimeSpan.FromSeconds(5), CancellationToken.None);
        Assert.True(gate.HasWaiters);

        gate.Exit();
        Assert.True(await waiter);
        Assert.False(gate.HasWaiters);
        gate.Exit();
    }

    [Fact]
    public async Task TryEnter_WithZeroTimeout_FailsWhenBusyWithoutLeavingAWaiter()
    {
        var gate = new CatalogGenerationGate();
        Assert.True(await gate.TryEnterAsync(TimeSpan.Zero, CancellationToken.None));

        Assert.False(await gate.TryEnterAsync(TimeSpan.Zero, CancellationToken.None));
        Assert.False(gate.HasWaiters);
        gate.Exit();
    }
}
