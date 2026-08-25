using MediatR;

namespace Vidriera.Application.Auth;

public record RequestPasswordResetCommand(string Email) : IRequest;
