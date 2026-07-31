using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;
using Vidriera.Domain.Entities;

namespace Vidriera.Infrastructure.Persistence.Mappings;

public class CompanyMapping : ClassMapping<Company>
{
    public CompanyMapping()
    {
        Table("companies");

        Id(x => x.Id, m =>
        {
            m.Column("id");
            m.Generator(Generators.GuidComb);
        });

        Property(x => x.Name, m =>
        {
            m.Column("name");
            m.NotNullable(true);
            m.Length(200);
        });

        Property(x => x.IsActive, m => m.Column("is_active"));
        Property(x => x.CreatedAt, m => m.Column("created_at"));

        Property(x => x.LogoBlobKey, m =>
        {
            m.Column("logo_blob_key");
            m.Length(500);
        });

        Property(x => x.LogoContentType, m =>
        {
            m.Column("logo_content_type");
            m.Length(100);
        });

        Bag(x => x.Users, m =>
        {
            m.Key(k => k.Column("company_id"));
            m.Inverse(true);
            m.Cascade(Cascade.None);
        }, r => r.OneToMany());

        Bag(x => x.Products, m =>
        {
            m.Key(k => k.Column("company_id"));
            m.Inverse(true);
            m.Cascade(Cascade.None);
        }, r => r.OneToMany());

        Bag(x => x.GeneratedCatalogs, m =>
        {
            m.Key(k => k.Column("company_id"));
            m.Inverse(true);
            m.Cascade(Cascade.None);
        }, r => r.OneToMany());
    }
}
