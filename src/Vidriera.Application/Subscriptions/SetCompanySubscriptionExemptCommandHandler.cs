using MediatR;
using NHibernate;
using NHibernate.Linq;
using Vidriera.Application.Common;
using Vidriera.Domain.Entities;

namespace Vidriera.Application.Subscriptions;

public class SetCompanySubscriptionExemptCommandHandler : IRequestHandler<SetCompanySubscriptionExemptCommand>
{
    private readonly ISession _session;

    public SetCompanySubscriptionExemptCommandHandler(ISession session)
    {
        _session = session;
    }

    public async Task Handle(SetCompanySubscriptionExemptCommand request, CancellationToken cancellationToken)
    {
        var subscription = await _session.Query<CompanySubscription>().GetOrThrowAsync(
            s => s.Company.Id == request.CompanyId,
            ErrorMessages.CompanySubscriptionNotFound(request.CompanyId),
            cancellationToken);

        using var transaction = _session.BeginTransaction();

        subscription.IsExempt = request.IsExempt;
        subscription.UpdatedAt = DateTime.UtcNow;
        await _session.UpdateAsync(subscription, cancellationToken);

        if (request.IsExempt && !subscription.Company.IsActive)
        {
            subscription.Company.IsActive = true;
            await _session.UpdateAsync(subscription.Company, cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }
}
