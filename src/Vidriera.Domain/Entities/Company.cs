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

    public virtual IList<User> Users { get; set; } = new List<User>();
    public virtual IList<Product> Products { get; set; } = new List<Product>();
}
