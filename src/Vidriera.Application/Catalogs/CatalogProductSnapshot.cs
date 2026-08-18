namespace Vidriera.Application.Catalogs;

public record CatalogProductSnapshot(Guid Id, string Name, string? Code);

public record CatalogIndexEntry(string Name, int StartPage, int Level, bool IsProduct, Guid? ProductId = null);

public record CatalogSnapshot(
    IReadOnlyList<CatalogProductSnapshot> Products,
    IReadOnlyList<CatalogIndexEntry> IndexEntries);
