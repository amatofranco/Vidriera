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

        // Se lee el estado en vivo antes de cancelar: durante un cambio de plan la preapproval
        // principal ya está cancelada en MP, y la que hay que dar de baja es la pendiente —
        // si no, el cliente podría autorizarla después y la empresa se reactivaría sola.
        var current = await _mercadoPagoClient.GetPreapprovalAsync(subscription.PreapprovalId, cancellationToken);
        var preapproval = await PreapprovalCanceller.CancelIfNotFinalAsync(_mercadoPagoClient, current, cancellationToken);

        if (subscription.PendingPreapprovalId is not null)
        {
            var pending = await _mercadoPagoClient.GetPreapprovalAsync(subscription.PendingPreapprovalId, cancellationToken);
            await PreapprovalCanceller.CancelIfNotFinalAsync(_mercadoPagoClient, pending, cancellationToken);
        }

        using var transaction = _session.BeginTransaction();

        subscription.Status = preapproval.Status;
        subscription.UpdatedAt = DateTime.UtcNow;
        subscription.Company.IsActive = false;
        PreapprovalCanceller.ClearPendingPlan(subscription);
        await _session.UpdateAsync(subscription, cancellationToken);
        await _session.UpdateAsync(subscription.Company, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }
}
