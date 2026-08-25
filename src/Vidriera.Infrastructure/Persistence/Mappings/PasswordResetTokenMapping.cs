using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;
using Vidriera.Domain.Entities;

namespace Vidriera.Infrastructure.Persistence.Mappings;

public class PasswordResetTokenMapping : ClassMapping<PasswordResetToken>
{
    public PasswordResetTokenMapping()
    {
        Table("password_reset_tokens");

        Id(x => x.Id, m =>
        {
            m.Column("id");
            m.Generator(Generators.GuidComb);
        });

        ManyToOne(x => x.User, m =>
        {
            m.Column("user_id");
            m.NotNullable(true);
        });

        Property(x => x.TokenHash, m =>
        {
            m.Column("token_hash");
            m.NotNullable(true);
            m.Unique(true);
            m.Length(100);
        });

        Property(x => x.ExpiresAt, m => m.Column("expires_at"));
        Property(x => x.UsedAt, m => m.Column("used_at"));
        Property(x => x.CreatedAt, m => m.Column("created_at"));
    }
}
