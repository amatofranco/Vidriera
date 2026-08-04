using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;
using Vidriera.Domain.Entities;

namespace Vidriera.Infrastructure.Persistence.Mappings;

public class SectionMapping : ClassMapping<Section>
{
    public SectionMapping()
    {
        Table("sections");

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

        Property(x => x.CoverPdfBlobKey, m =>
        {
            m.Column("cover_pdf_blob_key");
            m.Length(500);
        });

        Property(x => x.CoverPdfOriginalName, m =>
        {
            m.Column("cover_pdf_original_name");
            m.Length(300);
        });

        Property(x => x.SortOrder, m => m.Column("sort_order"));
    }
}
