using MediatR;

namespace Vidriera.Application.Subscriptions;

public record ChangeCompanyPlanCommand(Guid CompanyId, string PayerEmail, string NewPlan) : IRequest<ChangeCompanyPlanResult>;

public record ChangeCompanyPlanResult(string SubscriptionLinkUrl, DateTime EffectiveDate, DateTimeOffset? OldPreapprovalEndDateConfirmed);
