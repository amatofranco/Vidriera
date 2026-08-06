using MediatR;

namespace Vidriera.Application.Catalogs;

public record GetCurrentCatalogQuery(Guid CompanyId) : IRequest<GenerateCatalogResult?>;
