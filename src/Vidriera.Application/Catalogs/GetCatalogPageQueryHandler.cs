using MediatR;
using NHibernate;
using Vidriera.Application.Abstractions;
using Vidriera.Application.Common;
using Vidriera.Application.Common.Exceptions;
using Vidriera.Domain.Entities;

namespace Vidriera.Application.Catalogs;

public class GetCatalogPageQueryHandler : IRequestHandler<GetCatalogPageQuery, CatalogPageResult>
{
    private readonly ISession _session;
    private readonly IBlobStorageService _blobStorageService;

    public GetCatalogPageQueryHandler(ISession session, IBlobStorageService blobStorageService)
    {
        _session = session;
        _blobStorageService = blobStorageService;
    }

    public async Task<CatalogPageResult> Handle(GetCatalogPageQuery request, CancellationToken cancellationToken)
    {
        var catalog = await _session.GetAsync<GeneratedCatalog>(request.CatalogId, cancellationToken);

        if (catalog is null)
        {
            throw new NotFoundException(ErrorMessages.CatalogNotFound(request.CatalogId));
        }

        if (request.PageNumber < 1 || request.PageNumber > catalog.RasterizedPageCount)
        {
            throw new NotFoundException(ErrorMessages.CatalogPageNotFound(request.CatalogId, request.PageNumber));
        }

        var blobKey = request.Hd
            ? BlobKeys.GeneratedCatalogPageHd(catalog.Company.Id, catalog.Id, request.PageNumber)
            : BlobKeys.GeneratedCatalogPage(catalog.Company.Id, catalog.Id, request.PageNumber);

        try
        {
            var (content, contentType) = await _blobStorageService.DownloadWithContentTypeAsync(blobKey, cancellationToken);
            return new CatalogPageResult(content, contentType);
        }
        catch (FileNotFoundException) when (request.Hd)
        {
            throw new NotFoundException(ErrorMessages.CatalogPageNotFound(request.CatalogId, request.PageNumber));
        }
    }
}
