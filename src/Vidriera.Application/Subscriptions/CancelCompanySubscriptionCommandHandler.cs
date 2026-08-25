using MediatR;
using NHibernate;
using NHibernate.Linq;
using Vidriera.Application.Abstractions;
using Vidriera.Application.Common;
using Vidriera.Domain.Entities;

namespace Vidriera.Application.Subscriptions;

public class CancelCompanySubscriptionCommandHandler : IRequestHandler<CancelCompanySubscriptionCommand>
{
    private readonly ISession _session;
    private readonly IMercadoPagoClient _mercadoPagoClient;

    public CancelCompanySubscriptionCommandHandler(ISession session, IMercadoPagoClient mercadoPagoClient)
    {
        _session = session;
        _mercadoPagoClient = mercadoPagoClient;
    }

    public async Task Handle(CancelCompanySubscriptionCommand request, CancellationToken cancellationToken)
    {
        var subscription = await _session.Query<CompanySubscription>().GetOrThrowAsync(
            s => s.Company.Id == request.CompanyId,
            ErrorMessages.CompanySubscriptionNotFound(request.CompanyId),
            cancellationToken);

        var preapproval = await _mercadoPagoClient.CancelPreapprovalAsync(subscription.PreapprovalId, cancellationToken);

        using var transaction = _session.BeginTransaction();

        subscription.Status = preapproval.Status;
        subscription.UpdatedAt = DateTime.UtcNow;
        subscription.Company.IsActive = false;
        await _session.UpdateAsync(subscription, cancellationToken);
        await _session.UpdateAsync(subscription.Company, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }
}
