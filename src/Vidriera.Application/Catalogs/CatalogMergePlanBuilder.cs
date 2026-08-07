using Vidriera.Domain.Entities;

namespace Vidriera.Application.Catalogs;

internal abstract record MergeEntry;
internal sealed record SectionCoverEntry(Section Section) : MergeEntry;
internal sealed record ProductEntry(Product Product) : MergeEntry;

internal static class CatalogMergePlanBuilder
{
    public static IReadOnlyList<MergeEntry> BuildEntries(
        IReadOnlyList<Section> allSections,
        IReadOnlyList<Product> allProducts,
        HashSet<Guid> selectedIds)
    {
        var topLevel = BuildTopLevelSequence(allSections, allProducts);
        return BuildMergeEntries(topLevel, allProducts, allSections, selectedIds).ToList();
    }

    public static List<CatalogSectionSnapshot> BuildSectionsSnapshot(
        IReadOnlyList<int> pageCounts,
        IReadOnlyList<Section?> coverMarkers)
    {
        var sectionsSnapshot = new List<CatalogSectionSnapshot>();
        var pageCursor = 0;

        for (var i = 0; i < pageCounts.Count; i++)
        {
            if (coverMarkers[i] is { } coverSection)
            {
                sectionsSnapshot.Add(new CatalogSectionSnapshot(coverSection.Name, pageCursor + 1, coverSection.ParentSection is not null));
            }
            pageCursor += pageCounts[i];
        }

        return sectionsSnapshot;
    }

    private static IEnumerable<object> BuildTopLevelSequence(IReadOnlyList<Section> sections, IReadOnlyList<Product> allProducts)
    {
        var topLevelSections = sections.Where(s => s.ParentSection is null);
        var looseProducts = allProducts.Where(p => p.Section is null);
        return topLevelSections.Cast<object>()
            .Concat(looseProducts.Cast<object>())
            .OrderBy(item => item switch
            {
                Section s => s.SortOrder,
                Product p => p.SortOrder,
                _ => 0
            });
    }

    private static IEnumerable<MergeEntry> BuildMergeEntries(
        IEnumerable<object> topLevel,
        IReadOnlyList<Product> allProducts,
        IReadOnlyList<Section> allSections,
        HashSet<Guid> selectedIds)
    {
        foreach (var item in topLevel)
        {
            switch (item)
            {
                case Section section:
                    foreach (var entry in BuildSectionEntries(section, allProducts, allSections, selectedIds))
                    {
                        yield return entry;
                    }
                    break;

                case Product product when selectedIds.Contains(product.Id):
                    yield return new ProductEntry(product);
                    break;
            }
        }
    }

    private static IEnumerable<MergeEntry> BuildSectionEntries(
        Section section,
        IReadOnlyList<Product> allProducts,
        IReadOnlyList<Section> allSections,
        HashSet<Guid> selectedIds)
    {
        var childSections = allSections.Where(s => s.ParentSection?.Id == section.Id);
        var directProducts = allProducts.Where(p => p.Section?.Id == section.Id);

        var children = childSections.Cast<object>()
            .Concat(directProducts.Cast<object>())
            .OrderBy(item => item switch
            {
                Section s => s.SortOrder,
                Product p => p.SortOrder,
                _ => 0
            });

        var entries = new List<MergeEntry>();
        foreach (var child in children)
        {
            switch (child)
            {
                case Section subSection:
                    entries.AddRange(BuildLeafSectionEntries(subSection, allProducts, selectedIds));
                    break;

                case Product product when selectedIds.Contains(product.Id):
                    entries.Add(new ProductEntry(product));
                    break;
            }
        }

        if (entries.Count == 0)
        {
            yield break;
        }

        yield return new SectionCoverEntry(section);
        foreach (var entry in entries)
        {
            yield return entry;
        }
    }

    private static IEnumerable<MergeEntry> BuildLeafSectionEntries(
        Section section,
        IReadOnlyList<Product> allProducts,
        HashSet<Guid> selectedIds)
    {
        var members = allProducts
            .Where(p => p.Section?.Id == section.Id && selectedIds.Contains(p.Id))
            .OrderBy(p => p.SortOrder)
            .ToList();

        if (members.Count == 0)
        {
            yield break;
        }

        yield return new SectionCoverEntry(section);
        foreach (var member in members)
        {
            yield return new ProductEntry(member);
        }
    }
}
