using MediatR;
using NHibernate;
using Vidriera.Domain.Entities;

namespace Vidriera.Application.Catalogs;

public class GetCompanyCatalogVersionQueryHandler : IRequestHandler<GetCompanyCatalogVersionQuery, Guid?>
{
    private readonly ISession _session;

    public GetCompanyCatalogVersionQueryHandler(ISession session)
    {
        _session = session;
    }

    public async Task<Guid?> Handle(GetCompanyCatalogVersionQuery request, CancellationToken cancellationToken)
    {
        var company = await _session.GetAsync<Company>(request.CompanyId, cancellationToken);
        return company?.CurrentCatalogId;
    }
}
