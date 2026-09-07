using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;
using Vidriera.Domain.Entities;

namespace Vidriera.Infrastructure.Persistence.Mappings;

public class CatalogGenerationJobMapping : ClassMapping<CatalogGenerationJob>
{
    public CatalogGenerationJobMapping()
    {
        Table("catalog_generation_jobs");

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

        Property(x => x.ShowPrices, m => m.Column("show_prices"));

        Property(x => x.Status, m =>
        {
            m.Column("status");
            m.NotNullable(true);
            m.Length(20);
        });

        Property(x => x.Stage, m => m.Column("stage"));

        Property(x => x.ProgressCurrent, m => m.Column("progress_current"));
        Property(x => x.ProgressTotal, m => m.Column("progress_total"));

        Property(x => x.ResultCatalogId, m => m.Column("result_catalog_id"));
        Property(x => x.ResultUrl, m => m.Column("result_url"));
        Property(x => x.ErrorMessage, m => m.Column("error_message"));

        Property(x => x.CreatedAt, m => m.Column("created_at"));
        Property(x => x.UpdatedAt, m => m.Column("updated_at"));
    }
}
