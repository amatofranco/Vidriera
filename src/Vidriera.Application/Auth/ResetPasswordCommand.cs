using MediatR;

namespace Vidriera.Application.Auth;

public record ResetPasswordCommand(string Token, string NewPassword) : IRequest;
