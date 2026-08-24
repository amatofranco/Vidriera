using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;
using Vidriera.Domain.Entities;

namespace Vidriera.Infrastructure.Persistence.Mappings;

public class ProcessedMercadoPagoPaymentMapping : ClassMapping<ProcessedMercadoPagoPayment>
{
    public ProcessedMercadoPagoPaymentMapping()
    {
        Table("processed_mercadopago_payments");

        Id(x => x.Id, m =>
        {
            m.Column("id");
            m.Generator(Generators.GuidComb);
        });

        Property(x => x.PaymentId, m =>
        {
            m.Column("payment_id");
            m.NotNullable(true);
            m.Unique(true);
            m.Length(100);
        });

        Property(x => x.ProcessedAt, m => m.Column("processed_at"));
    }
}
