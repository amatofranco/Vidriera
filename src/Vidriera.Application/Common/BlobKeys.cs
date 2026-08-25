namespace Vidriera.Application.Common;

internal static class BlobKeys
{
    public static string ItemSheet(Guid companyId, Guid itemId) =>
        $"companies/{companyId}/items/{itemId}/{Guid.NewGuid()}.pdf";

    public static string SectionCover(Guid companyId, Guid sectionId) =>
        $"companies/{companyId}/sections/{sectionId}/{Guid.NewGuid()}.pdf";

    public static string CompanyLogo(Guid companyId) =>
        $"companies/{companyId}/logo";

    public static string GeneratedCatalog(Guid companyId) =>
        $"companies/{companyId}/catalogs/{Guid.NewGuid()}.pdf";

    public static string GeneratedCatalogPage(Guid companyId, Guid catalogId, int pageNumber) =>
        $"companies/{companyId}/catalogs/{catalogId}/pages/{pageNumber}.jpg";
}
