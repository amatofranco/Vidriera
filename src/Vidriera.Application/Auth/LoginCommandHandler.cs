using MediatR;
using Microsoft.Extensions.Options;
using NHibernate;
using NHibernate.Linq;
using Vidriera.Application.Abstractions;
using Vidriera.Application.Common;
using Vidriera.Application.Common.Exceptions;
using Vidriera.Application.Subscriptions;
using Vidriera.Domain.Entities;

namespace Vidriera.Application.Auth;

public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResult>
{
    private readonly ISession _session;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly MercadoPagoOptions _mercadoPagoOptions;

    public LoginCommandHandler(
        ISession session,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IOptions<MercadoPagoOptions> mercadoPagoOptions)
    {
        _session = session;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _mercadoPagoOptions = mercadoPagoOptions.Value;
    }

    public async Task<LoginResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _session.Query<User>()
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive, cancellationToken);

        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedException(ErrorMessages.InvalidCredentials);
        }

        var subscription = await _session.Query<CompanySubscription>()
            .FirstOrDefaultAsync(s => s.Company.Id == user.Company.Id, cancellationToken);

        if (subscription is not null && !HasAccess(user.Company, subscription))
        {
            throw new UnauthorizedException(ErrorMessages.SubscriptionAccessExpired);
        }

        var token = _jwtTokenGenerator.GenerateToken(user.Id, user.Company.Id, user.Email);

        return new LoginResult(token, user.Id, user.Company.Id, user.Company.Name, user.Name, user.Email, subscription?.Plan);
    }

    private bool HasAccess(Company company, CompanySubscription subscription)
    {
        if (subscription.IsExempt)
        {
            return true;
        }

        if (!company.IsActive)
        {
            return false;
        }

        if (subscription.AccessExpiresAt is null)
        {
            return false;
        }

        var accessDeadline = subscription.AccessExpiresAt.Value.AddDays(_mercadoPagoOptions.GracePeriodDays);
        return DateTime.UtcNow <= accessDeadline;
    }
}
