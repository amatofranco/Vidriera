using MediatR;

namespace Vidriera.Application.Catalogs;

public record GetCatalogGenerationJobStatusQuery(Guid JobId, Guid CompanyId) : IRequest<CatalogGenerationJobStatusResult>;

public record CatalogGenerationJobStatusResult(
    Guid JobId,
    string Status,
    string? Stage,
    int Current,
    int Total,
    GenerateCatalogResult? Result,
    string? ErrorMessage);
