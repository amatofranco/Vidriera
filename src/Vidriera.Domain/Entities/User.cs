namespace Vidriera.Domain.Entities;

public class User
{
    public virtual Guid Id { get; set; }
    public virtual Company Company { get; set; } = null!;
    public virtual string Email { get; set; } = null!;
    public virtual string Name { get; set; } = null!;
    public virtual string PasswordHash { get; set; } = null!;
    public virtual bool IsActive { get; set; }
}
