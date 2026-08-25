namespace Vidriera.Domain.Entities;

public class PasswordResetToken
{
    public virtual Guid Id { get; set; }
    public virtual User User { get; set; } = null!;
    public virtual string TokenHash { get; set; } = null!;
    public virtual DateTime ExpiresAt { get; set; }
    public virtual DateTime? UsedAt { get; set; }
    public virtual DateTime CreatedAt { get; set; }
}
