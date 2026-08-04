namespace Vidriera.Application.Common;

/// <summary>
/// Builds the R2 object key for every kind of file this app stores, so the
/// "companies/{companyId}/..." shape only has to be gotten right in one place.
/// </summary>
internal static class BlobKeys
{
    public static string ProductSheet(Guid companyId, Guid productId) =>
        $"companies/{companyId}/products/{productId}/{Guid.NewGuid()}.pdf";

    public static string SectionCover(Guid companyId, Guid sectionId) =>
        $"companies/{companyId}/sections/{sectionId}/{Guid.NewGuid()}.pdf";

    public static string CompanyLogo(Guid companyId) =>
        $"companies/{companyId}/logo";

    public static string GeneratedCatalog(Guid companyId) =>
        $"companies/{companyId}/catalogs/{Guid.NewGuid()}.pdf";
}
