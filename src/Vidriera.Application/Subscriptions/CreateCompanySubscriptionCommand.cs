using MediatR;

namespace Vidriera.Application.Subscriptions;

public record CreateCompanySubscriptionCommand(
    Guid CompanyId,
    string PayerEmail,
    string Plan) : IRequest<CreateCompanySubscriptionResult>;

public record CreateCompanySubscriptionResult(string SubscriptionLinkUrl);
