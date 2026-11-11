using Vidriera.Application.Subscriptions;
using Vidriera.Domain.Entities;

namespace Vidriera.Application.Tests.Subscriptions;

public class SubscriptionAccessTests
{
    private const int GraceDays = 5;
    private static readonly DateTime Now = new(2026, 11, 11, 12, 0, 0, DateTimeKind.Utc);

    private static (Company, CompanySubscription) Build(string status, DateTime? expiresAt, string? pendingPlan = null)
    {
        var company = new Company { IsActive = true };
        var subscription = new CompanySubscription
        {
            Company = company,
            Status = status,
            AccessExpiresAt = expiresAt,
            PendingPlan = pendingPlan
        };
        return (company, subscription);
    }

    [Fact]
    public void Authorized_WithinGracePeriod_HasAccess()
    {
        var (company, subscription) = Build("authorized", Now.AddDays(-3));

        Assert.True(SubscriptionAccess.HasAccess(company, subscription, GraceDays, Now));
    }

    [Fact]
    public void Authorized_AfterGracePeriod_HasNoAccess()
    {
        var (company, subscription) = Build("authorized", Now.AddDays(-6));

        Assert.False(SubscriptionAccess.HasAccess(company, subscription, GraceDays, Now));
    }

    [Theory]
    [InlineData("cancelled")]
    [InlineData("paused")]
    [InlineData("finished")]
    public void Stopped_KeepsAccessUntilPaidPeriodEnds(string status)
    {
        var (company, subscription) = Build(status, Now.AddDays(10));

        Assert.True(SubscriptionAccess.HasAccess(company, subscription, GraceDays, Now));
    }

    [Theory]
    [InlineData("cancelled")]
    [InlineData("paused")]
    [InlineData("finished")]
    public void Stopped_GetsNoGracePeriod(string status)
    {
        var (company, subscription) = Build(status, Now.AddDays(-1));

        Assert.False(SubscriptionAccess.HasAccess(company, subscription, GraceDays, Now));
    }

    [Fact]
    public void CancelledAsPartOfPlanChange_KeepsGracePeriod()
    {
        var (company, subscription) = Build("cancelled", Now.AddDays(-1), pendingPlan: SubscriptionPlans.Premium);

        Assert.True(SubscriptionAccess.HasAccess(company, subscription, GraceDays, Now));
    }

    [Fact]
    public void NeverPaid_HasNoAccess()
    {
        var (company, subscription) = Build("pending", null);

        Assert.False(SubscriptionAccess.HasAccess(company, subscription, GraceDays, Now));
    }

    [Fact]
    public void Exempt_AlwaysHasAccess()
    {
        var (company, subscription) = Build("cancelled", null);
        company.IsActive = false;
        subscription.IsExempt = true;

        Assert.True(SubscriptionAccess.HasAccess(company, subscription, GraceDays, Now));
    }
}
