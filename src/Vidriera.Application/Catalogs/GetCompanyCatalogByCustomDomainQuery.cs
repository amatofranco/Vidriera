using MediatR;

namespace Vidriera.Application.Catalogs;

public record GetCompanyCatalogByCustomDomainQuery(string Domain) : IRequest<GeneratedCatalogViewDto>;
