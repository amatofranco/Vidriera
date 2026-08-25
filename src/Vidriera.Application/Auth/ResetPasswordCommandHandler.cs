using System.Security.Cryptography;
using System.Text;
using MediatR;
using NHibernate;
using NHibernate.Linq;
using Vidriera.Application.Abstractions;
using Vidriera.Application.Common;
using Vidriera.Application.Common.Exceptions;
using Vidriera.Domain.Entities;

namespace Vidriera.Application.Auth;

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand>
{
    private readonly ISession _session;
    private readonly IPasswordHasher _passwordHasher;

    public ResetPasswordCommandHandler(ISession session, IPasswordHasher passwordHasher)
    {
        _session = session;
        _passwordHasher = passwordHasher;
    }

    public async Task Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(request.Token)));
        var now = DateTime.UtcNow;

        var resetToken = await _session.Query<PasswordResetToken>()
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash && t.UsedAt == null && t.ExpiresAt > now, cancellationToken);

        if (resetToken is null)
        {
            throw new ValidationException(ErrorMessages.InvalidOrExpiredResetToken);
        }

        using var transaction = _session.BeginTransaction();

        resetToken.User.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        resetToken.UsedAt = now;
        await _session.UpdateAsync(resetToken.User, cancellationToken);
        await _session.UpdateAsync(resetToken, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }
}
