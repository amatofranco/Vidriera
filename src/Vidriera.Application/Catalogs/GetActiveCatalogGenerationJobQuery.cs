using MediatR;

namespace Vidriera.Application.Catalogs;

public record GetActiveCatalogGenerationJobQuery(Guid CompanyId) : IRequest<CatalogGenerationJobStatusResult?>;
