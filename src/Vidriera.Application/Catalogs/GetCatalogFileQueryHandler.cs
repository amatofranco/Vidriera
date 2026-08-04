using MediatR;
using NHibernate;
using Vidriera.Application.Abstractions;
using Vidriera.Application.Common;
using Vidriera.Application.Common.Exceptions;
using Vidriera.Domain.Entities;
using Vidriera.Domain.Enums;

namespace Vidriera.Application.Catalogs;

public class GetCatalogFileQueryHandler : IRequestHandler<GetCatalogFileQuery, CatalogFileResult>
{
    private readonly ISession _session;
    private readonly IBlobStorageService _blobStorageService;

    public GetCatalogFileQueryHandler(ISession session, IBlobStorageService blobStorageService)
    {
        _session = session;
        _blobStorageService = blobStorageService;
    }

    public async Task<CatalogFileResult> Handle(GetCatalogFileQuery request, CancellationToken cancellationToken)
    {
        var catalog = await _session.GetAsync<GeneratedCatalog>(request.Id, cancellationToken);

        if (catalog is null)
        {
            throw new NotFoundException(ErrorMessages.CatalogNotFound(request.Id));
        }

        if (catalog.Status == CatalogStatus.Revoked)
        {
            throw new CatalogGoneException(ErrorMessages.CatalogRevoked);
        }

        if (catalog.Status == CatalogStatus.Expired
            || (catalog.ExpiresAt.HasValue && catalog.ExpiresAt.Value < DateTime.UtcNow))
        {
            throw new CatalogGoneException(ErrorMessages.CatalogExpired);
        }

        var content = await _blobStorageService.DownloadAsync(catalog.GeneratedPdfBlobKey, cancellationToken);
        return new CatalogFileResult(content, "application/pdf", $"catalogo-{catalog.Id}.pdf");
    }
}
