namespace Vidriera.Domain.Entities;

public class Section
{
    public virtual Guid Id { get; set; }
    public virtual Company Company { get; set; } = null!;
    public virtual string Name { get; set; } = null!;
    public virtual string? CoverPdfBlobKey { get; set; }
    public virtual string? CoverPdfOriginalName { get; set; }
    public virtual int SortOrder { get; set; }
}
