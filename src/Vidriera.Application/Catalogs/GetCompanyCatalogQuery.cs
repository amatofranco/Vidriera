using MediatR;

namespace Vidriera.Application.Catalogs;

public record GetCompanyCatalogQuery(Guid CompanyId) : IRequest<GeneratedCatalogViewDto>;
