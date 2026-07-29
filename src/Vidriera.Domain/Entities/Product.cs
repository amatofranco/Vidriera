namespace Vidriera.Domain.Entities;

public class Product
{
    public virtual Guid Id { get; set; }
    public virtual Company Company { get; set; } = null!;
    public virtual string Name { get; set; } = null!;
    public virtual string Code { get; set; } = null!;
    public virtual string? SheetPdfBlobKey { get; set; }
    public virtual string? SheetPdfOriginalName { get; set; }
    public virtual bool HasStock { get; set; }
    public virtual bool IsActive { get; set; }
}
