using MediatR;
using NHibernate;
using NHibernate.Linq;
using Vidriera.Application.Common;
using Vidriera.Application.Common.Exceptions;
using Vidriera.Domain.Entities;

namespace Vidriera.Application.Catalogs;

public class GetCompanyCatalogBySlugQueryHandler : IRequestHandler<GetCompanyCatalogBySlugQuery, GeneratedCatalogViewDto>
{
    private readonly ISession _session;
    private readonly IMediator _mediator;

    public GetCompanyCatalogBySlugQueryHandler(ISession session, IMediator mediator)
    {
        _session = session;
        _mediator = mediator;
    }

    public async Task<GeneratedCatalogViewDto> Handle(GetCompanyCatalogBySlugQuery request, CancellationToken cancellationToken)
    {
        var company = await _session.Query<Company>()
            .FirstOrDefaultAsync(c => c.Slug == request.Slug, cancellationToken);

        if (company?.CurrentCatalogId is not { } catalogId)
        {
            throw new NotFoundException(ErrorMessages.CompanyCatalogNotFound);
        }

        return await _mediator.Send(new GetGeneratedCatalogQuery(catalogId), cancellationToken);
    }
}
