namespace Vidriera.Domain.Entities;

public class GeneratedCatalogProduct
{
    public virtual Guid Id { get; set; }
    public virtual GeneratedCatalog GeneratedCatalog { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;
}
