using Vidriera.Domain.Entities;

namespace Vidriera.Application.Catalogs;

internal abstract record MergeEntry;
internal sealed record SectionCoverEntry(Section Section) : MergeEntry;
internal sealed record ItemEntry(Item Item) : MergeEntry;

internal static class CatalogMergePlanBuilder
{
    public static IReadOnlyList<MergeEntry> BuildEntries(
        IReadOnlyList<Section> allSections,
        IReadOnlyList<Item> allItems,
        HashSet<Guid> selectedIds)
    {
        var topLevel = BuildTopLevelSequence(allSections, allItems);
        return BuildMergeEntries(topLevel, allItems, allSections, selectedIds).ToList();
    }

    public static string ComputeContentFingerprint(IReadOnlyList<MergeEntry> entries) =>
        string.Join("|", entries.Select(entry => entry switch
        {
            SectionCoverEntry cover => "S:" + cover.Section.CoverPdfBlobKey,
            ItemEntry itemEntry => "P:" + itemEntry.Item.SheetPdfBlobKey,
            _ => throw new InvalidOperationException("Unknown merge entry type.")
        }));

    public static List<CatalogIndexEntry> BuildIndexSnapshot(
        IReadOnlyList<MergeEntry> entries,
        IReadOnlyList<int> pageCounts,
        bool includePrices = false)
    {
        var indexSnapshot = new List<CatalogIndexEntry>();
        var pageCursor = 0;
        var physicalIndex = 0;

        for (var i = 0; i < entries.Count; i++)
        {
            switch (entries[i])
            {
                case SectionCoverEntry cover:
                    var hasOwnPage = cover.Section.CoverPdfBlobKey is not null;
                    indexSnapshot.Add(new CatalogIndexEntry(
                        cover.Section.Name,
                        pageCursor + 1,
                        cover.Section.ParentSection is not null ? 1 : 0,
                        IsItem: false));
                    if (hasOwnPage)
                    {
                        pageCursor += pageCounts[physicalIndex];
                        physicalIndex++;
                    }
                    break;

                case ItemEntry itemEntry:
                    var level = itemEntry.Item.Section?.ParentSection is not null
                        ? 2
                        : itemEntry.Item.Section is not null
                            ? 1
                            : 0;
                    indexSnapshot.Add(new CatalogIndexEntry(
                        itemEntry.Item.Name,
                        pageCursor + 1,
                        level,
                        IsItem: true,
                        ItemId: itemEntry.Item.Id,
                        Price: includePrices ? itemEntry.Item.Price : null));
                    pageCursor += pageCounts[physicalIndex];
                    physicalIndex++;
                    break;
            }
        }

        return indexSnapshot;
    }

    private static IEnumerable<object> BuildTopLevelSequence(IReadOnlyList<Section> sections, IReadOnlyList<Item> allItems)
    {
        var topLevelSections = sections.Where(s => s.ParentSection is null);
        var looseItems = allItems.Where(p => p.Section is null);
        return topLevelSections.Cast<object>()
            .Concat(looseItems.Cast<object>())
            .OrderBy(item => item switch
            {
                Section s => s.SortOrder,
                Item p => p.SortOrder,
                _ => 0
            });
    }

    private static IEnumerable<MergeEntry> BuildMergeEntries(
        IEnumerable<object> topLevel,
        IReadOnlyList<Item> allItems,
        IReadOnlyList<Section> allSections,
        HashSet<Guid> selectedIds)
    {
        foreach (var item in topLevel)
        {
            switch (item)
            {
                case Section section:
                    foreach (var entry in BuildSectionEntries(section, allItems, allSections, selectedIds))
                    {
                        yield return entry;
                    }
                    break;

                case Item catalogItem when selectedIds.Contains(catalogItem.Id):
                    yield return new ItemEntry(catalogItem);
                    break;
            }
        }
    }

    private static IEnumerable<MergeEntry> BuildSectionEntries(
        Section section,
        IReadOnlyList<Item> allItems,
        IReadOnlyList<Section> allSections,
        HashSet<Guid> selectedIds)
    {
        var childSections = allSections.Where(s => s.ParentSection?.Id == section.Id);
        var directItems = allItems.Where(p => p.Section?.Id == section.Id);

        var children = childSections.Cast<object>()
            .Concat(directItems.Cast<object>())
            .OrderBy(item => item switch
            {
                Section s => s.SortOrder,
                Item p => p.SortOrder,
                _ => 0
            });

        var entries = new List<MergeEntry>();
        foreach (var child in children)
        {
            switch (child)
            {
                case Section subSection:
                    entries.AddRange(BuildLeafSectionEntries(subSection, allItems, selectedIds));
                    break;

                case Item item when selectedIds.Contains(item.Id):
                    entries.Add(new ItemEntry(item));
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
        IReadOnlyList<Item> allItems,
        HashSet<Guid> selectedIds)
    {
        var members = allItems
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
            yield return new ItemEntry(member);
        }
    }
}
