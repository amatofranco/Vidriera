namespace Vidriera.Domain.Entities;

public class Section
{
    public virtual Guid Id { get; set; }
    public virtual Company Company { get; set; } = null!;
    public virtual string Name { get; set; } = null!;
    // Nullable to match the actual write lifecycle: the row is saved first (to get its
    // GuidComb id for the blob path) and these get filled in right after, same two-phase
    // create as Product.SheetPdfBlobKey.
    public virtual string? CoverPdfBlobKey { get; set; }
    public virtual string? CoverPdfOriginalName { get; set; }
    public virtual int SortOrder { get; set; }
}
