using Vidriera.Application.Abstractions;
using Vidriera.Domain.Entities;

namespace Vidriera.Application.Subscriptions;

internal static class PendingPlanPromoter
{
    public static bool TryPromoteIfAuthorized(CompanySubscription subscription, MercadoPagoPreapproval preapproval)
    {
        if (subscription.PendingPreapprovalId != preapproval.Id)
        {
            return false;
        }

        if (!preapproval.Status.Equals("authorized", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        subscription.Plan = subscription.PendingPlan!;
        subscription.PlanAmountUsd = subscription.PendingPlanAmountUsd!.Value;
        subscription.UsdArsRate = subscription.PendingUsdArsRate!.Value;
        subscription.AmountArs = subscription.PendingAmountArs!.Value;
        subscription.PreapprovalId = subscription.PendingPreapprovalId;
        subscription.Status = preapproval.Status;

        subscription.PendingPlan = null;
        subscription.PendingPlanAmountUsd = null;
        subscription.PendingUsdArsRate = null;
        subscription.PendingAmountArs = null;
        subscription.PendingPreapprovalId = null;

        return true;
    }
}
