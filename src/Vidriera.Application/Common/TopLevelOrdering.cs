using NHibernate;
using NHibernate.Linq;
using Vidriera.Domain.Entities;

namespace Vidriera.Application.Common;

internal static class TopLevelOrdering
{
    public static async Task<int> NextTopLevelSortOrderAsync(ISession session, Guid companyId, CancellationToken cancellationToken)
    {
        var maxLooseProduct = await session.Query<Product>()
            .Where(p => p.Company.Id == companyId && p.Section == null)
            .Select(p => (int?)p.SortOrder)
            .MaxAsync(cancellationToken) ?? -1;

        var maxSection = await session.Query<Section>()
            .Where(s => s.Company.Id == companyId)
            .Select(s => (int?)s.SortOrder)
            .MaxAsync(cancellationToken) ?? -1;

        return Math.Max(maxLooseProduct, maxSection) + 1;
    }

    public static async Task<int> NextSectionSortOrderAsync(ISession session, Guid sectionId, CancellationToken cancellationToken)
    {
        var max = await session.Query<Product>()
            .Where(p => p.Section != null && p.Section.Id == sectionId)
            .Select(p => (int?)p.SortOrder)
            .MaxAsync(cancellationToken) ?? -1;

        return max + 1;
    }
}
