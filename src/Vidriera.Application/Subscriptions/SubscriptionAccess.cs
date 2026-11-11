using Vidriera.Domain.Entities;

namespace Vidriera.Application.Subscriptions;

internal static class SubscriptionAccess
{
    public static bool HasAccess(Company company, CompanySubscription subscription, int gracePeriodDays, DateTime now)
    {
        if (subscription.IsExempt)
        {
            return true;
        }

        if (!company.IsActive || subscription.AccessExpiresAt is null)
        {
            return false;
        }

        var stoppedCharging = IsStopped(subscription.Status) && subscription.PendingPlan is null;
        var deadline = stoppedCharging
            ? subscription.AccessExpiresAt.Value
            : subscription.AccessExpiresAt.Value.AddDays(gracePeriodDays);

        return now <= deadline;
    }

    private static bool IsStopped(string status) =>
        status.Equals("cancelled", StringComparison.OrdinalIgnoreCase)
        || status.Equals("paused", StringComparison.OrdinalIgnoreCase)
        || status.Equals("finished", StringComparison.OrdinalIgnoreCase);
}
