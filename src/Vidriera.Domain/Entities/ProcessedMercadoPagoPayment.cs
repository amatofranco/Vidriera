namespace Vidriera.Domain.Entities;

public class ProcessedMercadoPagoPayment
{
    public virtual Guid Id { get; set; }
    public virtual string PaymentId { get; set; } = null!;
    public virtual DateTime ProcessedAt { get; set; }
}
