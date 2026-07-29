using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;
using Vidriera.Domain.Entities;

namespace Vidriera.Infrastructure.Persistence.Mappings;

public class ProductMapping : ClassMapping<Product>
{
    public ProductMapping()
    {
        Table("products");

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

        Property(x => x.Name, m =>
        {
            m.Column("name");
            m.NotNullable(true);
            m.Length(300);
        });

        Property(x => x.Code, m =>
        {
            m.Column("code");
            m.NotNullable(true);
            m.Length(100);
        });

        Property(x => x.SheetPdfBlobKey, m =>
        {
            m.Column("sheet_pdf_blob_key");
            m.Length(500);
        });

        Property(x => x.SheetPdfOriginalName, m =>
        {
            m.Column("sheet_pdf_original_name");
            m.Length(300);
        });

        Property(x => x.HasStock, m => m.Column("has_stock"));
        Property(x => x.IsActive, m => m.Column("is_active"));
    }
}
