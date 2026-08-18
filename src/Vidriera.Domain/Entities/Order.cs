namespace Vidriera.Domain.Entities;

public class Order
{
    public virtual Guid Id { get; set; }
    public virtual Company Company { get; set; } = null!;
    public virtual string BusinessName { get; set; } = null!;
    public virtual string? StoreName { get; set; }
    public virtual string Cuit { get; set; } = null!;
    public virtual string? VatCondition { get; set; }
    public virtual string? Phone { get; set; }
    public virtual string Email { get; set; } = null!;
    public virtual string? City { get; set; }
    public virtual string? Province { get; set; }
    public virtual string? Carrier { get; set; }
    public virtual string? DeliveryAddress { get; set; }
    public virtual string ItemsSnapshotJson { get; set; } = "[]";
    public virtual DateTime CreatedAt { get; set; }
}
