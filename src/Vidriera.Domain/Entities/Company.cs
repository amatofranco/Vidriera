namespace Vidriera.Domain.Entities;

public class Company
{
    public virtual Guid Id { get; set; }
    public virtual string Name { get; set; } = null!;
    public virtual bool IsActive { get; set; }
    public virtual DateTime CreatedAt { get; set; }
    public virtual string? LogoBlobKey { get; set; }
    public virtual string? LogoContentType { get; set; }
    public virtual Guid? CurrentCatalogId { get; set; }
    public virtual bool ShowCode { get; set; } = true;
    public virtual bool ShowPrice { get; set; } = true;
    public virtual bool ShowOrders { get; set; } = true;
    public virtual string? CoverLogoBlobKey { get; set; }
    public virtual string? CoverLogoContentType { get; set; }
    public virtual string? CatalogSubtitle { get; set; } = "Catálogo";
    public virtual string? Slug { get; set; }

    public virtual IList<User> Users { get; set; } = new List<User>();
    public virtual IList<Item> Items { get; set; } = new List<Item>();
}
