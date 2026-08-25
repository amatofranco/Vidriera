namespace Vidriera.Domain.Entities;

public class Item
{
    public virtual Guid Id { get; set; }
    public virtual Company Company { get; set; } = null!;
    public virtual string Name { get; set; } = null!;
    public virtual string? Code { get; set; }
    public virtual decimal? Price { get; set; }
    public virtual string? SheetPdfBlobKey { get; set; }
    public virtual string? SheetPdfOriginalName { get; set; }
    public virtual bool HasStock { get; set; }
    public virtual bool IsActive { get; set; }
    public virtual int SortOrder { get; set; }
    public virtual int PageCount { get; set; }
    public virtual Section? Section { get; set; }
}
