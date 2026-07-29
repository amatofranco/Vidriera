using MediatR;

namespace Vidriera.Application.Admin;

public record CreateUserCommand(Guid CompanyId, string Email, string Name, string Password) : IRequest<CreateUserResult>;

public record CreateUserResult(Guid UserId);
