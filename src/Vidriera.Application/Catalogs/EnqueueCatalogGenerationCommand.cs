using MediatR;

namespace Vidriera.Application.Catalogs;

public record EnqueueCatalogGenerationCommand(Guid CompanyId, Guid UserId, bool ShowPrices = false)
    : IRequest<EnqueueCatalogGenerationResult>;

public record EnqueueCatalogGenerationResult(Guid JobId);
