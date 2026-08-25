using MediatR;
using Microsoft.Extensions.Options;
using NHibernate;
using Vidriera.Domain.Entities;

namespace Vidriera.Application.Catalogs;

public class GetCurrentCatalogQueryHandler : IRequestHandler<GetCurrentCatalogQuery, GenerateCatalogResult?>
{
    private readonly ISession _session;
    private readonly CatalogOptions _options;

    public GetCurrentCatalogQueryHandler(ISession session, IOptions<CatalogOptions> options)
    {
        _session = session;
        _options = options.Value;
    }

    public async Task<GenerateCatalogResult?> Handle(GetCurrentCatalogQuery request, CancellationToken cancellationToken)
    {
        var company = await _session.GetAsync<Company>(request.CompanyId, cancellationToken);

        if (company?.CurrentCatalogId is not { } catalogId)
        {
            return null;
        }

        var url = $"{_options.PublicBaseUrl.TrimEnd('/')}/{request.CompanyId}";
        return new GenerateCatalogResult(catalogId, url);
    }
}
