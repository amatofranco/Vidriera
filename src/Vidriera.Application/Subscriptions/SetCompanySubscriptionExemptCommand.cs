using MediatR;

namespace Vidriera.Application.Subscriptions;

public record SetCompanySubscriptionExemptCommand(Guid CompanyId, bool IsExempt) : IRequest;
