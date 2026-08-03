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

        // Not mapped NotNullable even though the DB column is NOT NULL: the handler
        // saves the section first (to get its GuidComb id for the blob key path),
        // then uploads and sets these, then commits -- same two-phase order Product
        // already uses for SheetPdfBlobKey. Marking these NotNullable here made
        // NHibernate reject the Save() eagerly, before the value was ever set.
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
