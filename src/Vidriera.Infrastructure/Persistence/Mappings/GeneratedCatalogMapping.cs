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

        Property(x => x.ProductsSnapshotJson, m =>
        {
            m.Column("products_snapshot");
            m.NotNullable(true);
        });

        Property(x => x.RasterizedPageCount, m => m.Column("rasterized_page_count"));

        Property(x => x.ContentFingerprint, m =>
        {
            m.Column("content_fingerprint");
            m.NotNullable(true);
        });
    }
}
