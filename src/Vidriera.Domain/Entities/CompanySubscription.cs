namespace Vidriera.Domain.Entities;

public static class SubscriptionPlans
{
    public const string Basic = "Basic";
    public const string Premium = "Premium";
}

public class CompanySubscription
{
    public virtual Guid Id { get; set; }
    public virtual Company Company { get; set; } = null!;
    public virtual string Plan { get; set; } = null!;
    public virtual decimal PlanAmountUsd { get; set; }
    public virtual decimal UsdArsRate { get; set; }
    public virtual decimal AmountArs { get; set; }
    public virtual string PreapprovalId { get; set; } = null!;
    public virtual string Status { get; set; } = null!;
    public virtual DateTime? AccessExpiresAt { get; set; }
    public virtual bool IsExempt { get; set; }
    public virtual DateTime CreatedAt { get; set; }
    public virtual DateTime UpdatedAt { get; set; }
}
