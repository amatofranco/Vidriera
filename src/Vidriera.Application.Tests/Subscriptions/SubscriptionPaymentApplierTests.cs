using Vidriera.Application.Abstractions;
using Vidriera.Application.Subscriptions;

namespace Vidriera.Application.Tests.Subscriptions;

public class SubscriptionPaymentApplierTests
{
    private static readonly DateTime Now = new(2026, 11, 11, 12, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData("approved", true, false)]
    [InlineData("refunded", false, true)]
    [InlineData("charged_back", false, true)]
    [InlineData("pending", false, false)]
    [InlineData("rejected", false, false)]
    public void ClassifiesPaymentStatus(string status, bool approved, bool reverted)
    {
        var payment = new MercadoPagoPayment("1", status, null);

        Assert.Equal(approved, SubscriptionPaymentApplier.IsApproved(payment));
        Assert.Equal(reverted, SubscriptionPaymentApplier.IsReverted(payment));
    }

    [Fact]
    public void ExtendAccess_FromFutureExpiry_AddsAMonthToTheExpiry()
    {
        var expiry = Now.AddDays(10);

        Assert.Equal(expiry.AddMonths(1), SubscriptionPaymentApplier.ExtendAccess(expiry, Now));
    }

    [Fact]
    public void ExtendAccess_FromPastOrMissingExpiry_AddsAMonthFromNow()
    {
        Assert.Equal(Now.AddMonths(1), SubscriptionPaymentApplier.ExtendAccess(Now.AddDays(-3), Now));
        Assert.Equal(Now.AddMonths(1), SubscriptionPaymentApplier.ExtendAccess(null, Now));
    }

    [Fact]
    public void RevertAccess_UndoesOneMonth()
    {
        var expiry = Now.AddMonths(2);

        Assert.Equal(Now.AddMonths(1), SubscriptionPaymentApplier.RevertAccess(expiry));
    }
}
