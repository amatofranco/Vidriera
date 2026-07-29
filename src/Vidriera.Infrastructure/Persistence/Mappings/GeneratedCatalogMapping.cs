using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;
using Vidriera.Domain.Entities;

namespace Vidriera.Infrastructure.Persistence.Mappings;

public class GeneratedCatalogMapping : ClassMapping<GeneratedCatalog>
{
    public GeneratedCatalogMapping()
    {
        Table("generated_catalogs");

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

        ManyToOne(x => x.User, m =>
        {
            m.Column("user_id");
            m.NotNullable(true);
        });

        Property(x => x.GeneratedAt, m => m.Column("generated_at"));

        Property(x => x.GeneratedPdfBlobKey, m =>
        {
            m.Column("generated_pdf_blob_key");
            m.NotNullable(true);
            m.Length(500);
        });

        Property(x => x.ExpiresAt, m => m.Column("expires_at"));

        Property(x => x.Status, m => m.Column("status"));

        Bag(x => x.Products, m =>
        {
            m.Key(k => k.Column("generated_catalog_id"));
            m.Inverse(true);
            m.Cascade(Cascade.All | Cascade.DeleteOrphans);
        }, r => r.OneToMany());
    }
}
