using MediatR;

namespace Vidriera.Application.Catalogs;

public record GetCatalogHistoryQuery(Guid CompanyId) : IRequest<IReadOnlyList<CatalogHistoryItemDto>>;

public record CatalogHistoryItemDto(
    Guid Id,
    DateTime GeneratedAt,
    DateTime? ExpiresAt,
    bool IsExpired,
    string ViewUrl,
    int ProductCount);
