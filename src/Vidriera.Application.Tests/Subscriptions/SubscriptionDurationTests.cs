using Vidriera.Application.Common.Exceptions;
using Vidriera.Application.Subscriptions;

namespace Vidriera.Application.Tests.Subscriptions;

public class SubscriptionDurationTests
{
    private static readonly DateTime Now = new(2026, 11, 11, 15, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void NoDuration_MeansNoEndDate()
    {
        Assert.Null(SubscriptionDuration.EndDateFrom(Now, null));
    }

    [Fact]
    public void TwelveMonths_EndsTheDayBeforeTheThirteenthCharge()
    {
        var endDate = SubscriptionDuration.EndDateFrom(Now, 12);

        Assert.Equal(new DateTime(2027, 11, 10, 15, 0, 0, DateTimeKind.Utc), endDate);
        // El cobro 12 (now + 11 meses) entra; el 13 (now + 12 meses) queda afuera.
        Assert.True(Now.AddMonths(11) < endDate);
        Assert.True(Now.AddMonths(12) > endDate);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(SubscriptionDuration.MaxMonths + 1)]
    public void OutOfRangeDuration_IsRejected(int months)
    {
        Assert.Throws<ValidationException>(() => SubscriptionDuration.EndDateFrom(Now, months));
    }
}
