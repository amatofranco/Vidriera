using MediatR;

namespace Vidriera.Application.Subscriptions;

public record SyncCompanySubscriptionCommand(Guid CompanyId) : IRequest<SyncCompanySubscriptionResult>;

public record SyncCompanySubscriptionResult(string Status, DateTime? AccessExpiresAt, bool CompanyIsActive, string? PendingPlan);
