using MediatR;
using NHibernate;
using NHibernate.Linq;
using Vidriera.Domain.Entities;

namespace Vidriera.Application.Admin;

public class GetCompaniesQueryHandler : IRequestHandler<GetCompaniesQuery, IReadOnlyList<CompanyListItemDto>>
{
    private readonly ISession _session;

    public GetCompaniesQueryHandler(ISession session)
    {
        _session = session;
    }

    public async Task<IReadOnlyList<CompanyListItemDto>> Handle(GetCompaniesQuery request, CancellationToken cancellationToken)
    {
        return await _session.Query<Company>()
            .OrderBy(c => c.Name)
            .Select(c => new CompanyListItemDto(
                c.Id, c.Name, c.Slug, c.IsActive, c.CreatedAt, c.ShowCode, c.ShowPrice, c.ShowOrders, c.CoverLogoBlobKey != null, c.CatalogSubtitle, c.CustomDomain))
            .ToListAsync(cancellationToken);
    }
}
