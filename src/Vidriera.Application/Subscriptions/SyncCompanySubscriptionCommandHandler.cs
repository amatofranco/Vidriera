using MediatR;
using NHibernate;
using NHibernate.Linq;
using Vidriera.Application.Abstractions;
using Vidriera.Application.Common;
using Vidriera.Domain.Entities;

namespace Vidriera.Application.Subscriptions;

public class SyncCompanySubscriptionCommandHandler : IRequestHandler<SyncCompanySubscriptionCommand, SyncCompanySubscriptionResult>
{
    private readonly ISession _session;
    private readonly IMercadoPagoClient _mercadoPagoClient;

    public SyncCompanySubscriptionCommandHandler(ISession session, IMercadoPagoClient mercadoPagoClient)
    {
        _session = session;
        _mercadoPagoClient = mercadoPagoClient;
    }

    public async Task<SyncCompanySubscriptionResult> Handle(SyncCompanySubscriptionCommand request, CancellationToken cancellationToken)
    {
        var subscription = await _session.Query<CompanySubscription>().GetOrThrowAsync(
            s => s.Company.Id == request.CompanyId,
            ErrorMessages.CompanySubscriptionNotFound(request.CompanyId),
            cancellationToken);

        var preapproval = await _mercadoPagoClient.GetPreapprovalAsync(subscription.PreapprovalId, cancellationToken);
        var payments = await _mercadoPagoClient.SearchPaymentsByExternalReferenceAsync(
            request.CompanyId.ToString(),
            cancellationToken);

        using var transaction = _session.BeginTransaction();

        subscription.Status = preapproval.Status;
        subscription.UpdatedAt = DateTime.UtcNow;
        await _session.UpdateAsync(subscription, cancellationToken);

        if (preapproval.Status.Equals("cancelled", StringComparison.OrdinalIgnoreCase)
            || preapproval.Status.Equals("paused", StringComparison.OrdinalIgnoreCase))
        {
            subscription.Company.IsActive = false;
            await _session.UpdateAsync(subscription.Company, cancellationToken);
        }

        foreach (var payment in payments)
        {
            await SubscriptionPaymentApplier.ApplyIfApprovedAndNewAsync(_session, subscription, payment, cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);

        return new SyncCompanySubscriptionResult(subscription.Status, subscription.AccessExpiresAt, subscription.Company.IsActive);
    }
}
