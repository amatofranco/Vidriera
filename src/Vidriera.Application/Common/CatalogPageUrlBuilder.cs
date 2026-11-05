namespace Vidriera.Application.Common;

internal static class CatalogPageUrlBuilder
{
    public static string BuildPagesBaseUrl(
        string apiPublicBaseUrl, string pagesPublicBaseUrl, bool catalogPagesPublic, Guid companyId, Guid catalogId) =>
        catalogPagesPublic && !string.IsNullOrEmpty(pagesPublicBaseUrl)
            ? $"{pagesPublicBaseUrl.TrimEnd('/')}/{BlobKeys.GeneratedCatalogPagesPrefix(companyId, catalogId)}"
            : $"{apiPublicBaseUrl.TrimEnd('/')}/api/catalogs/{catalogId}/pages";

    public static string BuildPageUrl(string pagesBaseUrl, bool catalogPagesPublic, int pageNumber) =>
        catalogPagesPublic ? $"{pagesBaseUrl}/{pageNumber}.jpg" : $"{pagesBaseUrl}/{pageNumber}";
}
