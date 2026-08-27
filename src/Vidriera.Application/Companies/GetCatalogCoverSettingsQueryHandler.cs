using MediatR;
using NHibernate;
using Vidriera.Application.Common;
using Vidriera.Domain.Entities;

namespace Vidriera.Application.Companies;

public class GetCatalogCoverSettingsQueryHandler : IRequestHandler<GetCatalogCoverSettingsQuery, CatalogCoverSettingsDto>
{
    private readonly ISession _session;

    public GetCatalogCoverSettingsQueryHandler(ISession session)
    {
        _session = session;
    }

    public async Task<CatalogCoverSettingsDto> Handle(GetCatalogCoverSettingsQuery request, CancellationToken cancellationToken)
    {
        var company = await _session.GetOrThrowAsync<Company>(
            request.CompanyId,
            $"No existe la empresa {request.CompanyId}.",
            cancellationToken);

        return new CatalogCoverSettingsDto(company.CoverLogoBlobKey is not null, company.CatalogSubtitle, company.BackgroundBlobKey is not null);
    }
}
