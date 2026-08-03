using NHibernate;
using NHibernate.Linq;
using Vidriera.Domain.Entities;

namespace Vidriera.Application.Common;

/// <summary>
/// Sections and loose (unsectioned) products share one ordering space at the "top level"
/// of a company's catalog -- this is where that shared space's next value gets computed,
/// so every place that appends something to the end (create a section, detach a product,
/// reassign a product to no section) agrees on what "the end" means.
/// </summary>
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
