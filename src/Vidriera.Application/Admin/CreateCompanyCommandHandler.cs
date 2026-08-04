using MediatR;
using NHibernate;
using NHibernate.Linq;
using Vidriera.Application.Abstractions;
using Vidriera.Application.Common;
using Vidriera.Application.Common.Exceptions;
using Vidriera.Domain.Entities;

namespace Vidriera.Application.Admin;

public class CreateCompanyCommandHandler : IRequestHandler<CreateCompanyCommand, CreateCompanyResult>
{
    private readonly ISession _session;
    private readonly IPasswordHasher _passwordHasher;

    public CreateCompanyCommandHandler(ISession session, IPasswordHasher passwordHasher)
    {
        _session = session;
        _passwordHasher = passwordHasher;
    }

    public async Task<CreateCompanyResult> Handle(CreateCompanyCommand request, CancellationToken cancellationToken)
    {
        var emailInUse = await _session.Query<User>()
            .AnyAsync(u => u.Email == request.UserEmail, cancellationToken);

        if (emailInUse)
        {
            throw new ValidationException(ErrorMessages.EmailAlreadyRegistered(request.UserEmail));
        }

        var company = new Company
        {
            Id = Guid.NewGuid(),
            Name = request.CompanyName,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            Company = company,
            Email = request.UserEmail,
            Name = request.UserName,
            PasswordHash = _passwordHasher.Hash(request.UserPassword),
            IsActive = true
        };

        using var transaction = _session.BeginTransaction();
        await _session.SaveAsync(company, cancellationToken);
        await _session.SaveAsync(user, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new CreateCompanyResult(company.Id, user.Id);
    }
}
