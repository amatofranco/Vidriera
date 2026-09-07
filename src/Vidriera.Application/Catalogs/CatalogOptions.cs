namespace Vidriera.Application.Catalogs;

public class CatalogOptions
{
    public string PublicBaseUrl { get; set; } = null!;
    public int MaxTotalPages { get; set; } = 600;
    public long MaxTotalBytes { get; set; } = 200 * 1024 * 1024;
}
