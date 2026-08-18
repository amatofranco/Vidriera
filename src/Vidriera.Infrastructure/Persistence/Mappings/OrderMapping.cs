using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;
using Vidriera.Domain.Entities;

namespace Vidriera.Infrastructure.Persistence.Mappings;

public class OrderMapping : ClassMapping<Order>
{
    public OrderMapping()
    {
        Table("orders");

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

        Property(x => x.BusinessName, m =>
        {
            m.Column("business_name");
            m.NotNullable(true);
            m.Length(300);
        });

        Property(x => x.StoreName, m =>
        {
            m.Column("store_name");
            m.Length(300);
        });

        Property(x => x.Cuit, m =>
        {
            m.Column("cuit");
            m.NotNullable(true);
            m.Length(50);
        });

        Property(x => x.VatCondition, m =>
        {
            m.Column("vat_condition");
            m.Length(100);
        });

        Property(x => x.Phone, m =>
        {
            m.Column("phone");
            m.Length(100);
        });

        Property(x => x.Email, m =>
        {
            m.Column("email");
            m.NotNullable(true);
            m.Length(300);
        });

        Property(x => x.City, m =>
        {
            m.Column("city");
            m.Length(200);
        });

        Property(x => x.Province, m =>
        {
            m.Column("province");
            m.Length(200);
        });

        Property(x => x.Carrier, m =>
        {
            m.Column("carrier");
            m.Length(200);
        });

        Property(x => x.DeliveryAddress, m =>
        {
            m.Column("delivery_address");
            m.Length(500);
        });

        Property(x => x.ItemsSnapshotJson, m =>
        {
            m.Column("items_snapshot_json");
            m.NotNullable(true);
        });

        Property(x => x.CreatedAt, m => m.Column("created_at"));
    }
}
