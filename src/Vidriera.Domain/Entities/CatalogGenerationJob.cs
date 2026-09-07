namespace Vidriera.Domain.Entities;

public static class CatalogGenerationJobStatus
{
    public const string Pending = "Pending";
    public const string Running = "Running";
    public const string Succeeded = "Succeeded";
    public const string Failed = "Failed";
}

public class CatalogGenerationJob
{
    public virtual Guid Id { get; set; }
    public virtual Company Company { get; set; } = null!;
    public virtual User User { get; set; } = null!;
    public virtual bool ShowPrices { get; set; }
    public virtual string Status { get; set; } = CatalogGenerationJobStatus.Pending;
    public virtual string? Stage { get; set; }
    public virtual int ProgressCurrent { get; set; }
    public virtual int ProgressTotal { get; set; }
    public virtual Guid? ResultCatalogId { get; set; }
    public virtual string? ResultUrl { get; set; }
    public virtual string? ErrorMessage { get; set; }
    public virtual DateTime CreatedAt { get; set; }
    public virtual DateTime UpdatedAt { get; set; }
}
