using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;
using Vidriera.Domain.Entities;

namespace Vidriera.Infrastructure.Persistence.Mappings;

public class ItemMapping : ClassMapping<Item>
{
    public ItemMapping()
    {
        Table("items");

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
            m.Length(50);
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

        Property(x => x.Price, m => m.Column("price"));

        Property(x => x.HasStock, m => m.Column("has_stock"));
        Property(x => x.IsActive, m => m.Column("is_active"));
        Property(x => x.SortOrder, m => m.Column("sort_order"));
        Property(x => x.PageCount, m => m.Column("page_count"));

        ManyToOne(x => x.Section, m =>
        {
            m.Column("section_id");
            m.NotNullable(false);
        });
    }
}
