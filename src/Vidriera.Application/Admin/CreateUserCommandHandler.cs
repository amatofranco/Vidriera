using MediatR;
using NHibernate;
using NHibernate.Linq;
using Vidriera.Application.Abstractions;
using Vidriera.Application.Common;
using Vidriera.Application.Common.Exceptions;
using Vidriera.Application.Subscriptions;
using Vidriera.Domain.Entities;

namespace Vidriera.Application.Admin;

public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, CreateUserResult>
{
    private readonly ISession _session;
    private readonly IPasswordHasher _passwordHasher;

    public CreateUserCommandHandler(ISession session, IPasswordHasher passwordHasher)
    {
        _session = session;
        _passwordHasher = passwordHasher;
    }

    public async Task<CreateUserResult> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var company = await _session.GetAsync<Company>(request.CompanyId, cancellationToken);
        if (company is null)
        {
            throw new NotFoundException(ErrorMessages.CompanyNotFound(request.CompanyId));
        }

        var emailInUse = await _session.Query<User>()
            .AnyAsync(u => u.Email == request.Email, cancellationToken);

        if (emailInUse)
        {
            throw new ValidationException(ErrorMessages.EmailAlreadyRegistered(request.Email));
        }

        await PlanLimitEnforcer.EnsureCanAddUserAsync(_session, request.CompanyId, cancellationToken);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Company = company,
            Email = request.Email,
            Name = request.Name,
            PasswordHash = _passwordHasher.Hash(request.Password),
            IsActive = true
        };

        using var transaction = _session.BeginTransaction();
        await _session.SaveAsync(user, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new CreateUserResult(user.Id);
    }
}
