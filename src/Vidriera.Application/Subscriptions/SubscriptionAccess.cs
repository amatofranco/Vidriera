using Vidriera.Domain.Entities;

namespace Vidriera.Application.Subscriptions;

internal static class SubscriptionAccess
{
    /// <summary>
    /// El acceso dura hasta donde está pago (AccessExpiresAt). Mientras la suscripción sigue
    /// cobrando se suman los días de gracia, para cubrir un cobro mensual que se demora. Si el
    /// cliente canceló o pausó, no va a entrar otro pago: el acceso termina justo al final del
    /// período pago, sin gracia. La excepción es un cambio de plan en curso: la preapproval vieja
    /// queda cancelada a propósito y el primer cobro de la nueva cae justo al vencimiento.
    /// </summary>
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
