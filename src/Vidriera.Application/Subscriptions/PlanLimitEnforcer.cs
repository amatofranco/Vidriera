using NHibernate;
using NHibernate.Linq;
using Vidriera.Application.Common;
using Vidriera.Application.Common.Exceptions;
using Vidriera.Domain.Entities;

namespace Vidriera.Application.Subscriptions;

internal static class PlanLimitEnforcer
{
    public static async Task EnsureCanAddProductAsync(ISession session, Guid companyId, CancellationToken cancellationToken)
    {
        var max = await GetLimitAsync(session, companyId, PlanLimits.MaxProducts, cancellationToken);
        if (max is null)
        {
            return;
        }

        var count = await session.Query<Product>().CountAsync(p => p.Company.Id == companyId && p.IsActive, cancellationToken);
        if (count >= max)
        {
            throw new ValidationException(ErrorMessages.ProductLimitReached(max.Value));
        }
    }

    public static async Task EnsureCanAddUserAsync(ISession session, Guid companyId, CancellationToken cancellationToken)
    {
        var max = await GetLimitAsync(session, companyId, PlanLimits.MaxUsers, cancellationToken);
        if (max is null)
        {
            return;
        }

        var count = await session.Query<User>().CountAsync(u => u.Company.Id == companyId && u.IsActive, cancellationToken);
        if (count >= max)
        {
            throw new ValidationException(ErrorMessages.UserLimitReached(max.Value));
        }
    }

    private static async Task<int?> GetLimitAsync(
        ISession session,
        Guid companyId,
        Func<string, int?> resolveLimit,
        CancellationToken cancellationToken)
    {
        var subscription = await session.Query<CompanySubscription>()
            .FirstOrDefaultAsync(s => s.Company.Id == companyId, cancellationToken);

        return subscription is null || subscription.IsExempt ? null : resolveLimit(subscription.Plan);
    }
}
