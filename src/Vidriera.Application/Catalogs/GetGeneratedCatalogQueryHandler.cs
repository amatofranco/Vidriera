using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Options;
using NHibernate;
using Vidriera.Application.Common.Exceptions;
using Vidriera.Domain.Entities;
using Vidriera.Domain.Enums;

namespace Vidriera.Application.Catalogs;

public class GetGeneratedCatalogQueryHandler : IRequestHandler<GetGeneratedCatalogQuery, GeneratedCatalogViewDto>
{
    private readonly ISession _session;
    private readonly CatalogOptions _options;

    public GetGeneratedCatalogQueryHandler(ISession session, IOptions<CatalogOptions> options)
    {
        _session = session;
        _options = options.Value;
    }

    public async Task<GeneratedCatalogViewDto> Handle(GetGeneratedCatalogQuery request, CancellationToken cancellationToken)
    {
        var catalog = await _session.GetAsync<GeneratedCatalog>(request.Id, cancellationToken);

        if (catalog is null)
        {
            throw new NotFoundException($"No existe el catálogo {request.Id}.");
        }

        if (catalog.Status == CatalogStatus.Revoked)
        {
            throw new CatalogGoneException("Este catálogo fue revocado.");
        }

        if (catalog.Status == CatalogStatus.Expired
            || (catalog.ExpiresAt.HasValue && catalog.ExpiresAt.Value < DateTime.UtcNow))
        {
            throw new CatalogGoneException("Este catálogo ya expiró.");
        }

        var fileUrl = $"{_options.PublicBaseUrl.TrimEnd('/')}/api/catalogs/{catalog.Id}/file";

        // Catalogs generated before the sections-index feature existed have ProductsSnapshotJson
        // as a bare array (the old shape), which fails to deserialize as CatalogSnapshot -- treat
        // that the same as "no sections", rather than a 500 on an old, otherwise-valid catalog.
        CatalogSnapshot snapshot;
        try
        {
            snapshot = JsonSerializer.Deserialize<CatalogSnapshot>(catalog.ProductsSnapshotJson)
                ?? new CatalogSnapshot([], []);
        }
        catch (JsonException)
        {
            snapshot = new CatalogSnapshot([], []);
        }

        return new GeneratedCatalogViewDto(
            catalog.Id,
            catalog.GeneratedAt,
            catalog.ExpiresAt,
            fileUrl,
            catalog.Company.Name,
            snapshot.Sections);
    }
}
