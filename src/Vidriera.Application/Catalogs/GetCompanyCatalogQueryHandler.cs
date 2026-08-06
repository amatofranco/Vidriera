using MediatR;
using NHibernate;
using Vidriera.Application.Common;
using Vidriera.Application.Common.Exceptions;
using Vidriera.Domain.Entities;

namespace Vidriera.Application.Catalogs;

public class GetCompanyCatalogQueryHandler : IRequestHandler<GetCompanyCatalogQuery, GeneratedCatalogViewDto>
{
    private readonly ISession _session;
    private readonly IMediator _mediator;

    public GetCompanyCatalogQueryHandler(ISession session, IMediator mediator)
    {
        _session = session;
        _mediator = mediator;
    }

    public async Task<GeneratedCatalogViewDto> Handle(GetCompanyCatalogQuery request, CancellationToken cancellationToken)
    {
        var company = await _session.GetAsync<Company>(request.CompanyId, cancellationToken);

        if (company?.CurrentCatalogId is not { } catalogId)
        {
            throw new NotFoundException(ErrorMessages.CompanyCatalogNotFound);
        }

        return await _mediator.Send(new GetGeneratedCatalogQuery(catalogId), cancellationToken);
    }
}
