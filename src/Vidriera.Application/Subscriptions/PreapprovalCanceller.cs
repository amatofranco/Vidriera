using Vidriera.Application.Abstractions;
using Vidriera.Domain.Entities;

namespace Vidriera.Application.Subscriptions;

internal static class PreapprovalCanceller
{
    public static bool IsAuthorized(MercadoPagoPreapproval preapproval) =>
        preapproval.Status.Equals("authorized", StringComparison.OrdinalIgnoreCase);

    public static bool IsFinal(MercadoPagoPreapproval preapproval) =>
        preapproval.Status.Equals("cancelled", StringComparison.OrdinalIgnoreCase)
        || preapproval.Status.Equals("finished", StringComparison.OrdinalIgnoreCase);

    public static async Task<MercadoPagoPreapproval> CancelIfNotFinalAsync(
        IMercadoPagoClient client,
        MercadoPagoPreapproval preapproval,
        CancellationToken cancellationToken) =>
        IsFinal(preapproval)
            ? preapproval
            : await client.CancelPreapprovalAsync(preapproval.Id, cancellationToken);

    public static void ClearPendingPlan(CompanySubscription subscription)
    {
        subscription.PendingPlan = null;
        subscription.PendingPlanAmountUsd = null;
        subscription.PendingUsdArsRate = null;
        subscription.PendingAmountArs = null;
        subscription.PendingPreapprovalId = null;
    }
}
