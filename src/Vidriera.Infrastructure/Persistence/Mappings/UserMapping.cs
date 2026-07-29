using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;
using Vidriera.Domain.Entities;

namespace Vidriera.Infrastructure.Persistence.Mappings;

public class UserMapping : ClassMapping<User>
{
    public UserMapping()
    {
        Table("users");

        Id(x => x.Id, m =>
        {
            m.Column("id");
            m.Generator(Generators.GuidComb);
        });

        ManyToOne(x => x.Company, m =>
        {
            m.Column("company_id");
            m.NotNullable(true);
        });

        Property(x => x.Email, m =>
        {
            m.Column("email");
            m.NotNullable(true);
            m.Length(320);
            m.Unique(true);
        });

        Property(x => x.Name, m =>
        {
            m.Column("name");
            m.NotNullable(true);
            m.Length(200);
        });

        Property(x => x.PasswordHash, m =>
        {
            m.Column("password_hash");
            m.NotNullable(true);
            m.Length(500);
        });

        Property(x => x.IsActive, m => m.Column("is_active"));
    }
}
