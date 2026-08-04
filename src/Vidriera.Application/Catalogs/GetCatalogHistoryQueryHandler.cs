using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Options;
using NHibernate;
using NHibernate.Linq;
using Vidriera.Domain.Entities;
using Vidriera.Domain.Enums;

namespace Vidriera.Application.Catalogs;

public class GetCatalogHistoryQueryHandler : IRequestHandler<GetCatalogHistoryQuery, IReadOnlyList<CatalogHistoryItemDto>>
{
    private readonly ISession _session;
    private readonly CatalogOptions _options;

    public GetCatalogHistoryQueryHandler(ISession session, IOptions<CatalogOptions> options)
    {
        _session = session;
        _options = options.Value;
    }

    public async Task<IReadOnlyList<CatalogHistoryItemDto>> Handle(GetCatalogHistoryQuery request, CancellationToken cancellationToken)
    {
        var catalogs = await _session.Query<GeneratedCatalog>()
            .Where(c => c.Company.Id == request.CompanyId && c.Status != CatalogStatus.Revoked)
            .OrderByDescending(c => c.GeneratedAt)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;

        return catalogs
            .Select(c => new CatalogHistoryItemDto(
                c.Id,
                c.GeneratedAt,
                c.ExpiresAt,
                c.Status == CatalogStatus.Expired || (c.ExpiresAt.HasValue && c.ExpiresAt.Value < now),
                $"{_options.PublicBaseUrl.TrimEnd('/')}/api/catalogs/{c.Id}",
                CountProducts(c.ProductsSnapshotJson)))
            .ToList();
    }

    private static int CountProducts(string json)
    {
        try
        {
            var snapshot = JsonSerializer.Deserialize<CatalogSnapshot>(json);
            if (snapshot is not null) return snapshot.Products.Count;
        }
        catch (JsonException)
        {
        }

        try
        {
            var legacy = JsonSerializer.Deserialize<List<CatalogProductSnapshot>>(json);
            return legacy?.Count ?? 0;
        }
        catch (JsonException)
        {
            return 0;
        }
    }
}
