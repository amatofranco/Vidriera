using System.Security.Cryptography;
using System.Text;
using MediatR;
using Microsoft.Extensions.Options;
using NHibernate;
using NHibernate.Linq;
using Vidriera.Application.Abstractions;
using Vidriera.Domain.Entities;

namespace Vidriera.Application.Auth;

public class RequestPasswordResetCommandHandler : IRequestHandler<RequestPasswordResetCommand>
{
    private readonly ISession _session;
    private readonly IEmailSender _emailSender;
    private readonly FrontendOptions _frontendOptions;

    public RequestPasswordResetCommandHandler(ISession session, IEmailSender emailSender, IOptions<FrontendOptions> frontendOptions)
    {
        _session = session;
        _emailSender = emailSender;
        _frontendOptions = frontendOptions.Value;
    }

    public async Task Handle(RequestPasswordResetCommand request, CancellationToken cancellationToken)
    {
        var user = await _session.Query<User>()
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive, cancellationToken);

        // Nunca revelar si el email existe o no — mismo motivo que el login no distingue
        // "no existe" de "contraseña incorrecta".
        if (user is null)
        {
            return;
        }

        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
        var tokenHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

        var resetToken = new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            User = user,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            CreatedAt = DateTime.UtcNow
        };

        using var transaction = _session.BeginTransaction();
        await _session.SaveAsync(resetToken, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var resetLink = $"{_frontendOptions.BaseUrl}/reset-password?token={rawToken}";
        var html = $"""
            <p>Recibimos un pedido para restablecer tu contraseña de Vidriera.</p>
            <p><a href="{resetLink}">Elegí una contraseña nueva</a></p>
            <p>Este link vence en 1 hora. Si no fuiste vos, ignorá este mail.</p>
            """;

        await _emailSender.SendAsync(user.Email, "Restablecer tu contraseña de Vidriera", html, cancellationToken);
    }
}
