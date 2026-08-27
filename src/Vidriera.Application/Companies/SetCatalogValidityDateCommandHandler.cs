using MediatR;
using NHibernate;
using Vidriera.Application.Common;
using Vidriera.Domain.Entities;

namespace Vidriera.Application.Companies;

public class SetCatalogValidityDateCommandHandler : IRequestHandler<SetCatalogValidityDateCommand>
{
    private readonly ISession _session;

    public SetCatalogValidityDateCommandHandler(ISession session)
    {
        _session = session;
    }

    public async Task Handle(SetCatalogValidityDateCommand request, CancellationToken cancellationToken)
    {
        var company = await _session.GetOrThrowAsync<Company>(
            request.CompanyId,
            $"No existe la empresa {request.CompanyId}.",
            cancellationToken);

        company.CustomValidityDate = request.CustomDate;
        company.ShowValidityDate = request.Show;

        await _session.UpdateInTransactionAsync(company, cancellationToken);
    }
}
