using MediatR;
using NHibernate;
using NHibernate.Linq;
using Vidriera.Application.Common;
using Vidriera.Application.Common.Exceptions;
using Vidriera.Domain.Entities;

namespace Vidriera.Application.Catalogs;

public class GetCompanyCatalogByCustomDomainQueryHandler : IRequestHandler<GetCompanyCatalogByCustomDomainQuery, GeneratedCatalogViewDto>
{
    private readonly ISession _session;
    private readonly IMediator _mediator;

    public GetCompanyCatalogByCustomDomainQueryHandler(ISession session, IMediator mediator)
    {
        _session = session;
        _mediator = mediator;
    }

    public async Task<GeneratedCatalogViewDto> Handle(GetCompanyCatalogByCustomDomainQuery request, CancellationToken cancellationToken)
    {
        var domain = request.Domain.ToLowerInvariant();
        var company = await _session.Query<Company>()
            .FirstOrDefaultAsync(c => c.CustomDomain == domain, cancellationToken);

        if (company?.CurrentCatalogId is not { } catalogId)
        {
            throw new NotFoundException(ErrorMessages.CompanyCatalogNotFound);
        }

        return await _mediator.Send(new GetGeneratedCatalogQuery(catalogId), cancellationToken);
    }
}
