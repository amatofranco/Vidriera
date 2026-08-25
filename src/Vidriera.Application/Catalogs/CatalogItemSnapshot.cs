namespace Vidriera.Application.Catalogs;

public record CatalogItemSnapshot(Guid Id, string Name, string? Code);

public record CatalogIndexEntry(string Name, int StartPage, int Level, bool IsItem, Guid? ItemId = null, decimal? Price = null);

public record CatalogSnapshot(
    IReadOnlyList<CatalogItemSnapshot> Items,
    IReadOnlyList<CatalogIndexEntry> IndexEntries);
