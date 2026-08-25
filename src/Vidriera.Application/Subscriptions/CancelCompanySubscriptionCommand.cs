using MediatR;

namespace Vidriera.Application.Subscriptions;

public record CancelCompanySubscriptionCommand(Guid CompanyId) : IRequest;
