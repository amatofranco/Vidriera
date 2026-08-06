using MediatR;

namespace Vidriera.Application.Catalogs;

public record GetGeneratedCatalogQuery(Guid Id) : IRequest<GeneratedCatalogViewDto>;

public record GeneratedCatalogViewDto(
    Guid Id,
    DateTime GeneratedAt,
    string FileUrl,
    string CompanyName,
    IReadOnlyList<CatalogSectionSnapshot> Sections,
    int RasterizedPageCount);
