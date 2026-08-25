using NHibernate;
using NHibernate.Linq;
using Vidriera.Application.Abstractions;
using Vidriera.Domain.Entities;

namespace Vidriera.Application.Subscriptions;

internal static class SubscriptionPaymentApplier
{
    public static async Task<bool> ApplyIfApprovedAndNewAsync(
        ISession session,
        CompanySubscription subscription,
        MercadoPagoPayment payment,
        CancellationToken cancellationToken)
    {
        if (!payment.Status.Equals("approved", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var alreadyProcessed = await session.Query<ProcessedMercadoPagoPayment>()
            .AnyAsync(p => p.PaymentId == payment.Id, cancellationToken);
        if (alreadyProcessed)
        {
            return false;
        }

        var now = DateTime.UtcNow;
        var extendFrom = subscription.AccessExpiresAt.HasValue && subscription.AccessExpiresAt.Value > now
            ? subscription.AccessExpiresAt.Value
            : now;

        subscription.AccessExpiresAt = extendFrom.AddMonths(1);
        subscription.UpdatedAt = now;
        subscription.Company.IsActive = true;
        await session.UpdateAsync(subscription, cancellationToken);
        await session.UpdateAsync(subscription.Company, cancellationToken);

        await session.SaveAsync(new ProcessedMercadoPagoPayment
        {
            Id = Guid.NewGuid(),
            PaymentId = payment.Id,
            ProcessedAt = now
        }, cancellationToken);

        return true;
    }
}
