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

        Property(x => x.CurrentCatalogId, m => m.Column("current_catalog_id"));
        Property(x => x.ShowCode, m => m.Column("show_code"));
        Property(x => x.ShowPrice, m => m.Column("show_price"));
        Property(x => x.ShowOrders, m => m.Column("show_orders"));

        Property(x => x.CoverLogoBlobKey, m =>
        {
            m.Column("cover_logo_blob_key");
            m.Length(500);
        });

        Property(x => x.CoverLogoContentType, m =>
        {
            m.Column("cover_logo_content_type");
            m.Length(100);
        });

        Property(x => x.CatalogSubtitle, m =>
        {
            m.Column("catalog_subtitle");
            m.Length(100);
        });

        Property(x => x.BackgroundBlobKey, m =>
        {
            m.Column("background_blob_key");
            m.Length(500);
        });

        Property(x => x.BackgroundContentType, m =>
        {
            m.Column("background_content_type");
            m.Length(100);
        });

        Property(x => x.Slug, m =>
        {
            m.Column("slug");
            m.Length(100);
            m.Unique(true);
        });

        Property(x => x.CustomDomain, m =>
        {
            m.Column("custom_domain");
            m.Length(255);
            m.Unique(true);
        });

        Property(x => x.CustomValidityDate, m => m.Column("custom_validity_date"));

        Property(x => x.ShowValidityDate, m => m.Column("show_validity_date"));

        Bag(x => x.Users, m =>
        {
            m.Key(k => k.Column("company_id"));
            m.Inverse(true);
            m.Cascade(Cascade.None);
        }, r => r.OneToMany());

        Bag(x => x.Items, m =>
        {
            m.Key(k => k.Column("company_id"));
            m.Inverse(true);
            m.Cascade(Cascade.None);
        }, r => r.OneToMany());
    }
}
