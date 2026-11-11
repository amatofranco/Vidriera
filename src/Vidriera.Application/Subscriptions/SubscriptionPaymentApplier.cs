using NHibernate;
using NHibernate.Linq;
using Vidriera.Application.Abstractions;
using Vidriera.Domain.Entities;

namespace Vidriera.Application.Subscriptions;

internal static class SubscriptionPaymentApplier
{
    /// <summary>
    /// Aplica un pago de MercadoPago a la suscripción: un pago aprobado suma un mes de acceso;
    /// uno reembolsado o con contracargo, que ya se había sumado, lo resta. Es idempotente: cada
    /// pago suma y resta como mucho una vez, así que se puede llamar desde el webhook y desde el
    /// /sync cuantas veces haga falta.
    /// </summary>
    public static async Task<bool> ApplyAsync(
        ISession session,
        CompanySubscription subscription,
        MercadoPagoPayment payment,
        CancellationToken cancellationToken)
    {
        if (IsApproved(payment))
        {
            return await ApplyIfNewAsync(session, subscription, payment, cancellationToken);
        }

        if (IsReverted(payment))
        {
            return await RevertIfAppliedAsync(session, subscription, payment, cancellationToken);
        }

        return false;
    }

    private static async Task<bool> ApplyIfNewAsync(
        ISession session,
        CompanySubscription subscription,
        MercadoPagoPayment payment,
        CancellationToken cancellationToken)
    {
        var alreadyProcessed = await session.Query<ProcessedMercadoPagoPayment>()
            .AnyAsync(p => p.PaymentId == payment.Id, cancellationToken);
        if (alreadyProcessed)
        {
            return false;
        }

        var now = DateTime.UtcNow;
        subscription.AccessExpiresAt = ExtendAccess(subscription.AccessExpiresAt, now);
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

    private static async Task<bool> RevertIfAppliedAsync(
        ISession session,
        CompanySubscription subscription,
        MercadoPagoPayment payment,
        CancellationToken cancellationToken)
    {
        // Si el pago nunca se sumó (el reembolso llegó antes que la aprobación, o nunca estuvo
        // aprobado), no hay nada que restar — y como ya no va a volver a figurar como aprobado,
        // tampoco se va a sumar después.
        var processed = await session.Query<ProcessedMercadoPagoPayment>()
            .FirstOrDefaultAsync(p => p.PaymentId == payment.Id, cancellationToken);
        if (processed is null || processed.RevertedAt is not null || subscription.AccessExpiresAt is null)
        {
            return false;
        }

        var now = DateTime.UtcNow;
        subscription.AccessExpiresAt = RevertAccess(subscription.AccessExpiresAt.Value);
        subscription.UpdatedAt = now;
        processed.RevertedAt = now;
        await session.UpdateAsync(subscription, cancellationToken);
        await session.UpdateAsync(processed, cancellationToken);

        return true;
    }

    public static bool IsApproved(MercadoPagoPayment payment) =>
        payment.Status.Equals("approved", StringComparison.OrdinalIgnoreCase);

    // Los reembolsos parciales no cambian el status (sigue "approved"), así que no restan.
    public static bool IsReverted(MercadoPagoPayment payment) =>
        payment.Status.Equals("refunded", StringComparison.OrdinalIgnoreCase)
        || payment.Status.Equals("charged_back", StringComparison.OrdinalIgnoreCase);

    public static DateTime ExtendAccess(DateTime? accessExpiresAt, DateTime now) =>
        (accessExpiresAt.HasValue && accessExpiresAt.Value > now ? accessExpiresAt.Value : now).AddMonths(1);

    public static DateTime RevertAccess(DateTime accessExpiresAt) => accessExpiresAt.AddMonths(-1);
}
