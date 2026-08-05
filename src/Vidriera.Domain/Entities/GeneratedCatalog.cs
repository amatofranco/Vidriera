using Vidriera.Domain.Enums;

namespace Vidriera.Domain.Entities;

public class GeneratedCatalog
{
    public virtual Guid Id { get; set; }
    public virtual Company Company { get; set; } = null!;
    public virtual User User { get; set; } = null!;
    public virtual DateTime GeneratedAt { get; set; }
    public virtual string GeneratedPdfBlobKey { get; set; } = null!;
    public virtual DateTime? ExpiresAt { get; set; }
    public virtual CatalogStatus Status { get; set; }

    public virtual string ProductsSnapshotJson { get; set; } = "[]";
    public virtual int RasterizedPageCount { get; set; }
}
