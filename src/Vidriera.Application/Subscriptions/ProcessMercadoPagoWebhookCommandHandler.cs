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
            PendingPlanPromoter.TryPromoteIfAuthorized(subscription, preapproval);
            subscription.UpdatedAt = DateTime.UtcNow;
            await _session.UpdateAsync(subscription, cancellationToken);
        }
        else
        {
            subscription.Status = preapproval.Status;
            subscription.UpdatedAt = DateTime.UtcNow;
            await _session.UpdateAsync(subscription, cancellationToken);

            if (preapproval.Status.Equals("cancelled", StringComparison.OrdinalIgnoreCase)
                || preapproval.Status.Equals("paused", StringComparison.OrdinalIgnoreCase))
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
