using Vidriera.Application.Catalogs;
using Vidriera.Domain.Entities;

namespace Vidriera.Application.Tests.Catalogs;

public class CatalogMergePlanBuilderTests
{
    private static Item MakeItem(string name, int sortOrder, Section? section = null, string? sheetKey = "sheet.pdf") => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        SortOrder = sortOrder,
        Section = section,
        SheetPdfBlobKey = sheetKey,
    };

    private static Section MakeSection(string name, int sortOrder, Section? parent = null, string? coverKey = "cover.pdf") => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        SortOrder = sortOrder,
        ParentSection = parent,
        CoverPdfBlobKey = coverKey,
    };

    [Fact]
    public void BuildEntries_OnlyIncludesSelectedLooseItems_InSortOrder()
    {
        var itemB = MakeItem("B", sortOrder: 2);
        var itemA = MakeItem("A", sortOrder: 1);
        var unselected = MakeItem("C", sortOrder: 3);
        var selectedIds = new HashSet<Guid> { itemA.Id, itemB.Id };

        var entries = CatalogMergePlanBuilder.BuildEntries([], [itemA, itemB, unselected], selectedIds);

        var items = Assert.IsType<List<MergeEntry>>(entries);
        Assert.Equal(2, items.Count);
        Assert.Equal(itemA.Id, Assert.IsType<ItemEntry>(items[0]).Item.Id);
        Assert.Equal(itemB.Id, Assert.IsType<ItemEntry>(items[1]).Item.Id);
    }

    [Fact]
    public void BuildEntries_OmitsSectionsWithNoSelectedMembers()
    {
        var section = MakeSection("Ofertas", sortOrder: 1);
        var item = MakeItem("Item sin seleccionar", sortOrder: 1, section);

        var entries = CatalogMergePlanBuilder.BuildEntries([section], [item], selectedIds: []);

        Assert.Empty(entries);
    }

    [Fact]
    public void BuildEntries_SectionWithSelectedMembers_YieldsCoverThenItemsInSortOrder()
    {
        var section = MakeSection("Ofertas", sortOrder: 1);
        var second = MakeItem("Segundo", sortOrder: 2, section);
        var first = MakeItem("Primero", sortOrder: 1, section);
        var selectedIds = new HashSet<Guid> { first.Id, second.Id };

        var entries = CatalogMergePlanBuilder.BuildEntries([section], [first, second], selectedIds);

        Assert.Equal(3, entries.Count);
        Assert.Equal(section.Id, Assert.IsType<SectionCoverEntry>(entries[0]).Section.Id);
        Assert.Equal(first.Id, Assert.IsType<ItemEntry>(entries[1]).Item.Id);
        Assert.Equal(second.Id, Assert.IsType<ItemEntry>(entries[2]).Item.Id);
    }

    [Fact]
    public void BuildEntries_NestedSubsectionWithSelectedMembers_YieldsParentAndChildCovers()
    {
        var parent = MakeSection("Catálogo general", sortOrder: 1);
        var child = MakeSection("Subsección", sortOrder: 1, parent);
        var item = MakeItem("Item", sortOrder: 1, child);
        var selectedIds = new HashSet<Guid> { item.Id };

        var entries = CatalogMergePlanBuilder.BuildEntries([parent, child], [item], selectedIds);

        Assert.Equal(3, entries.Count);
        Assert.Equal(parent.Id, Assert.IsType<SectionCoverEntry>(entries[0]).Section.Id);
        Assert.Equal(child.Id, Assert.IsType<SectionCoverEntry>(entries[1]).Section.Id);
        Assert.Equal(item.Id, Assert.IsType<ItemEntry>(entries[2]).Item.Id);
    }

    [Fact]
    public void BuildEntries_ParentSectionWithOnlyUnselectedSubsection_IsOmittedEntirely()
    {
        var parent = MakeSection("Catálogo general", sortOrder: 1);
        var child = MakeSection("Subsección", sortOrder: 1, parent);
        var item = MakeItem("Item sin seleccionar", sortOrder: 1, child);

        var entries = CatalogMergePlanBuilder.BuildEntries([parent, child], [item], selectedIds: []);

        Assert.Empty(entries);
    }

    [Fact]
    public void ComputeContentFingerprint_ChangesWhenSelectionChanges()
    {
        var item1 = MakeItem("Item 1", sortOrder: 1, sheetKey: "a.pdf");
        var item2 = MakeItem("Item 2", sortOrder: 2, sheetKey: "b.pdf");

        var withBoth = CatalogMergePlanBuilder.BuildEntries([], [item1, item2], [item1.Id, item2.Id]);
        var withOnlyFirst = CatalogMergePlanBuilder.BuildEntries([], [item1, item2], [item1.Id]);

        var fingerprintBoth = CatalogMergePlanBuilder.ComputeContentFingerprint(withBoth);
        var fingerprintOnlyFirst = CatalogMergePlanBuilder.ComputeContentFingerprint(withOnlyFirst);

        Assert.NotEqual(fingerprintBoth, fingerprintOnlyFirst);
    }

    [Fact]
    public void ComputeContentFingerprint_IsStableForTheSameSelection()
    {
        var item1 = MakeItem("Item 1", sortOrder: 1, sheetKey: "a.pdf");
        var item2 = MakeItem("Item 2", sortOrder: 2, sheetKey: "b.pdf");
        var selectedIds = new HashSet<Guid> { item1.Id, item2.Id };

        var first = CatalogMergePlanBuilder.ComputeContentFingerprint(
            CatalogMergePlanBuilder.BuildEntries([], [item1, item2], selectedIds));
        var second = CatalogMergePlanBuilder.ComputeContentFingerprint(
            CatalogMergePlanBuilder.BuildEntries([], [item1, item2], selectedIds));

        Assert.Equal(first, second);
    }

    [Fact]
    public void BuildIndexSnapshot_AdvancesPageCursorByEachEntrysPageCount()
    {
        var section = MakeSection("Ofertas", sortOrder: 1);
        var item = MakeItem("Item", sortOrder: 1, section);
        var entries = CatalogMergePlanBuilder.BuildEntries([section], [item], [item.Id]);

        var snapshot = CatalogMergePlanBuilder.BuildIndexSnapshot(entries, pageCounts: [1, 3]);

        Assert.Equal(2, snapshot.Count);
        Assert.Equal(1, snapshot[0].StartPage);
        Assert.False(snapshot[0].IsItem);
        Assert.Equal(2, snapshot[1].StartPage);
        Assert.True(snapshot[1].IsItem);
    }

    [Fact]
    public void BuildIndexSnapshot_SectionWithoutOwnCoverPage_DoesNotConsumeAPage()
    {
        var section = MakeSection("Ofertas", sortOrder: 1, coverKey: null);
        var item = MakeItem("Item", sortOrder: 1, section);
        var entries = CatalogMergePlanBuilder.BuildEntries([section], [item], [item.Id]);

        var snapshot = CatalogMergePlanBuilder.BuildIndexSnapshot(entries, pageCounts: [2]);

        Assert.Equal(1, snapshot[0].StartPage);
        Assert.Equal(1, snapshot[1].StartPage);
    }

    [Fact]
    public void BuildIndexSnapshot_OmitsPriceUnlessIncludePricesIsTrue()
    {
        var item = MakeItem("Item", sortOrder: 1);
        item.Price = 100m;
        var entries = CatalogMergePlanBuilder.BuildEntries([], [item], [item.Id]);

        var withoutPrices = CatalogMergePlanBuilder.BuildIndexSnapshot(entries, [1], includePrices: false);
        var withPrices = CatalogMergePlanBuilder.BuildIndexSnapshot(entries, [1], includePrices: true);

        Assert.Null(withoutPrices[0].Price);
        Assert.Equal(100m, withPrices[0].Price);
    }
}
