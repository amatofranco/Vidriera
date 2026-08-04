using MediatR;
using NHibernate;
using NHibernate.Linq;
using Vidriera.Application.Abstractions;
using Vidriera.Application.Common;
using Vidriera.Application.Common.Exceptions;
using Vidriera.Domain.Entities;

namespace Vidriera.Application.Auth;

public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResult>
{
    private readonly ISession _session;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public LoginCommandHandler(ISession session, IPasswordHasher passwordHasher, IJwtTokenGenerator jwtTokenGenerator)
    {
        _session = session;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<LoginResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _session.Query<User>()
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive, cancellationToken);

        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedException(ErrorMessages.InvalidCredentials);
        }

        var token = _jwtTokenGenerator.GenerateToken(user.Id, user.Company.Id, user.Email);

        return new LoginResult(token, user.Id, user.Company.Id, user.Company.Name, user.Name, user.Email);
    }
}
