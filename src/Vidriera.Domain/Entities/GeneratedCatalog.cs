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

    /// <summary>
    /// JSON snapshot of the products included at generation time (id, name, code) — not a live FK,
    /// so it survives products being deleted afterward and reflects their state at that moment.
    /// </summary>
    public virtual string ProductsSnapshotJson { get; set; } = "[]";
}
