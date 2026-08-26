namespace Vidriera.Application.Catalogs;

public static class CatalogShareUrl
{
    public static string Build(string publicBaseUrl, Guid companyId, string? slug)
    {
        var segment = string.IsNullOrWhiteSpace(slug) ? companyId.ToString() : slug;
        return $"{publicBaseUrl.TrimEnd('/')}/{segment}";
    }
}
