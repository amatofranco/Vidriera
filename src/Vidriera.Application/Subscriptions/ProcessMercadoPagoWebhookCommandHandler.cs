using MediatR;
using NHibernate;
using NHibernate.Linq;
using Vidriera.Application.Abstractions;
using Vidriera.Domain.Entities;

namespace Vidriera.Application.Subscriptions;

public class ProcessMercadoPagoWebhookCommandHandler : IRequestHandler<ProcessMercadoPagoWebhookCommand>
{
    private readonly ISession _session;
    private readonly IMercadoPagoClient _mercadoPagoClient;

    public ProcessMercadoPagoWebhookCommandHandler(ISession session, IMercadoPagoClient mercadoPagoClient)
    {
        _session = session;
        _mercadoPagoClient = mercadoPagoClient;
    }

    public async Task Handle(ProcessMercadoPagoWebhookCommand request, CancellationToken cancellationToken)
    {
        if (request.Type.Contains("preapproval", StringComparison.OrdinalIgnoreCase))
        {
            await HandlePreapprovalNotificationAsync(request.ResourceId, cancellationToken);
        }
        else if (request.Type.Equals("payment", StringComparison.OrdinalIgnoreCase))
        {
            await HandlePaymentNotificationAsync(request.ResourceId, cancellationToken);
        }
    }

    private async Task HandlePreapprovalNotificationAsync(string preapprovalId, CancellationToken cancellationToken)
    {
        var preapproval = await _mercadoPagoClient.GetPreapprovalAsync(preapprovalId, cancellationToken);

        var subscription = await _session.Query<CompanySubscription>()
            .FirstOrDefaultAsync(s => s.PreapprovalId == preapproval.Id || s.PendingPreapprovalId == preapproval.Id, cancellationToken);
        if (subscription is null)
        {
            return;
        }

        using var transaction = _session.BeginTransaction();

        if (subscription.PendingPreapprovalId == preapproval.Id)
        {
            if (PendingPlanPromoter.TryPromoteIfAuthorized(subscription, preapproval))
            {
                await _session.UpdateAsync(subscription.Company, cancellationToken);
            }

            subscription.UpdatedAt = DateTime.UtcNow;
            await _session.UpdateAsync(subscription, cancellationToken);
        }
        else
        {
            subscription.Status = preapproval.Status;
            subscription.UpdatedAt = DateTime.UtcNow;
            await _session.UpdateAsync(subscription, cancellationToken);

            // Si hay un cambio de plan en curso, esta preapproval se canceló a propósito como
            // parte del cambio (ver ChangeCompanyPlanCommandHandler) — no es que el cliente
            // canceló de verdad, así que no hay que cortarle el acceso. Se chequea PendingPlan
            // (se guarda ANTES de cancelar en MP) y no PendingPreapprovalId (se guarda después,
            // y este webhook puede llegar en el medio).
            var isCancellationPartOfPlanChange = subscription.PendingPlan is not null;

            if (!isCancellationPartOfPlanChange
                && (preapproval.Status.Equals("cancelled", StringComparison.OrdinalIgnoreCase)
                    || preapproval.Status.Equals("paused", StringComparison.OrdinalIgnoreCase)))
            {
                subscription.Company.IsActive = false;
                await _session.UpdateAsync(subscription.Company, cancellationToken);
            }
        }

        await transaction.CommitAsync(cancellationToken);
    }

    private async Task HandlePaymentNotificationAsync(string paymentId, CancellationToken cancellationToken)
    {
        var payment = await _mercadoPagoClient.GetPaymentAsync(paymentId, cancellationToken);

        if (payment.ExternalReference is null || !Guid.TryParse(payment.ExternalReference, out var companyId))
        {
            return;
        }

        var subscription = await _session.Query<CompanySubscription>()
            .FirstOrDefaultAsync(s => s.Company.Id == companyId, cancellationToken);
        if (subscription is null)
        {
            return;
        }

        using var transaction = _session.BeginTransaction();
        await SubscriptionPaymentApplier.ApplyIfApprovedAndNewAsync(_session, subscription, payment, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
