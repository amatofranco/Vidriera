using MediatR;

namespace Vidriera.Application.Auth;

public record LoginCommand(string Email, string Password) : IRequest<LoginResult>;

public record LoginResult(string Token, Guid UserId, Guid CompanyId, string Name, string Email);
