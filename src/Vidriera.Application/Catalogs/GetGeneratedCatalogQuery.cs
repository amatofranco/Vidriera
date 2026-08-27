using MediatR;
using Vidriera.Application.Orders;

namespace Vidriera.Application.Catalogs;

public record GetGeneratedCatalogQuery(Guid Id) : IRequest<GeneratedCatalogViewDto>;

public record GeneratedCatalogViewDto(
    Guid Id,
    Guid CompanyId,
    DateTime GeneratedAt,
    string FileUrl,
    string CompanyName,
    IReadOnlyList<CatalogIndexEntry> IndexEntries,
    int RasterizedPageCount,
    bool ShowOrders,
    bool HasCoverLogo,
    string? CatalogSubtitle,
    IReadOnlyList<OrderFormFieldDto> OrderFormFields);
