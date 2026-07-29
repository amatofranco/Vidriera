using MediatR;

namespace Vidriera.Application.Admin;

public record CreateCompanyCommand(
    string CompanyName,
    string UserEmail,
    string UserName,
    string UserPassword) : IRequest<CreateCompanyResult>;

public record CreateCompanyResult(Guid CompanyId, Guid UserId);
