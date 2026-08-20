using MediatR;

namespace Vidriera.Application.Catalogs;

public record GenerateCatalogCommand(
    Guid CompanyId,
    Guid UserId,
    bool ShowPrices = false,
    Func<CatalogGenerationProgress, Task>? OnProgress = null) : IRequest<GenerateCatalogResult>;

public record CatalogGenerationProgress(string Stage, int Current, int Total);

public record GenerateCatalogResult(Guid Id, string Url);
