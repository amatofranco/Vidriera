using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;
using Vidriera.Domain.Entities;

namespace Vidriera.Infrastructure.Persistence.Mappings;

public class OrderFormFieldMapping : ClassMapping<OrderFormField>
{
    public OrderFormFieldMapping()
    {
        Table("order_form_fields");

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

        Property(x => x.Label, m =>
        {
            m.Column("label");
            m.NotNullable(true);
            m.Length(100);
        });

        Property(x => x.FieldType, m =>
        {
            m.Column("field_type");
            m.NotNullable(true);
            m.Length(30);
        });

        Property(x => x.IsRequired, m => m.Column("is_required"));
        Property(x => x.SortOrder, m => m.Column("sort_order"));
    }
}
