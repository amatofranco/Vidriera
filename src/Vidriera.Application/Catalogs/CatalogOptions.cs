namespace Vidriera.Application.Catalogs;

public class CatalogOptions
{
    public int ExpirationDays { get; set; } = 5;
    public string PublicBaseUrl { get; set; } = null!;
}
