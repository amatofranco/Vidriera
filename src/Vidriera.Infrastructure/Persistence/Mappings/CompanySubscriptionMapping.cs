using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;
using Vidriera.Domain.Entities;

namespace Vidriera.Infrastructure.Persistence.Mappings;

public class CompanySubscriptionMapping : ClassMapping<CompanySubscription>
{
    public CompanySubscriptionMapping()
    {
        Table("company_subscriptions");

        Id(x => x.Id, m =>
        {
            m.Column("id");
            m.Generator(Generators.GuidComb);
        });

        ManyToOne(x => x.Company, m =>
        {
            m.Column("company_id");
            m.NotNullable(true);
            m.Unique(true);
        });

        Property(x => x.Plan, m =>
        {
            m.Column("plan");
            m.NotNullable(true);
            m.Length(20);
        });

        Property(x => x.PlanAmountUsd, m =>
        {
            m.Column("plan_amount_usd");
            m.NotNullable(true);
        });

        Property(x => x.UsdArsRate, m =>
        {
            m.Column("usd_ars_rate");
            m.NotNullable(true);
        });

        Property(x => x.AmountArs, m =>
        {
            m.Column("amount_ars");
            m.NotNullable(true);
        });

        Property(x => x.PreapprovalId, m =>
        {
            m.Column("preapproval_id");
            m.NotNullable(true);
            m.Length(100);
        });

        Property(x => x.Status, m =>
        {
            m.Column("status");
            m.NotNullable(true);
            m.Length(50);
        });

        Property(x => x.AccessExpiresAt, m => m.Column("access_expires_at"));
        Property(x => x.IsExempt, m => m.Column("is_exempt"));
        Property(x => x.CreatedAt, m => m.Column("created_at"));
        Property(x => x.UpdatedAt, m => m.Column("updated_at"));

        Property(x => x.PendingPlan, m =>
        {
            m.Column("pending_plan");
            m.Length(20);
        });
        Property(x => x.PendingPlanAmountUsd, m => m.Column("pending_plan_amount_usd"));
        Property(x => x.PendingUsdArsRate, m => m.Column("pending_usd_ars_rate"));
        Property(x => x.PendingAmountArs, m => m.Column("pending_amount_ars"));
        Property(x => x.PendingPreapprovalId, m =>
        {
            m.Column("pending_preapproval_id");
            m.Length(100);
        });
    }
}
