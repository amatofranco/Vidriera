using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Options;
using NHibernate;
using Vidriera.Application.Common;
using Vidriera.Application.Common.Exceptions;
using Vidriera.Application.Orders;
using Vidriera.Domain.Entities;

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
            throw new NotFoundException(ErrorMessages.CatalogNotFound(request.Id));
        }

        var fileUrl = $"{_options.PublicBaseUrl.TrimEnd('/')}/api/catalogs/{catalog.Id}/file";

        CatalogSnapshot snapshot;
        try
        {
            snapshot = JsonSerializer.Deserialize<CatalogSnapshot>(catalog.ItemsSnapshotJson)
                ?? new CatalogSnapshot([], []);
        }
        catch (JsonException)
        {
            snapshot = new CatalogSnapshot([], []);
        }

        var orderFormFields = await OrderFormFieldResolver.ResolveAsync(_session, catalog.Company.Id, cancellationToken);

        return new GeneratedCatalogViewDto(
            catalog.Id,
            catalog.Company.Id,
            catalog.GeneratedAt,
            fileUrl,
            catalog.Company.Name,
            snapshot.IndexEntries,
            catalog.RasterizedPageCount,
            catalog.Company.ShowOrders,
            orderFormFields);
    }
}
