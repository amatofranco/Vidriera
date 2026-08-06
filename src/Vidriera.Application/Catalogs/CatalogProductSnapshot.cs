namespace Vidriera.Application.Catalogs;

public record CatalogProductSnapshot(Guid Id, string Name, string? Code);

public record CatalogSectionSnapshot(string Name, int StartPage, bool IsSubsection);

public record CatalogSnapshot(
    IReadOnlyList<CatalogProductSnapshot> Products,
    IReadOnlyList<CatalogSectionSnapshot> Sections);
