using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;
using Vidriera.Domain.Entities;

namespace Vidriera.Infrastructure.Persistence.Mappings;

public class GeneratedCatalogProductMapping : ClassMapping<GeneratedCatalogProduct>
{
    public GeneratedCatalogProductMapping()
    {
        Table("generated_catalog_products");

        Id(x => x.Id, m =>
        {
            m.Column("id");
            m.Generator(Generators.GuidComb);
        });

        ManyToOne(x => x.GeneratedCatalog, m =>
        {
            m.Column("generated_catalog_id");
            m.NotNullable(true);
        });

        ManyToOne(x => x.Product, m =>
        {
            m.Column("product_id");
            m.NotNullable(true);
        });
    }
}
