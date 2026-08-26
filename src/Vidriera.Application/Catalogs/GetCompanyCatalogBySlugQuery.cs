using MediatR;

namespace Vidriera.Application.Catalogs;

public record GetCompanyCatalogBySlugQuery(string Slug) : IRequest<GeneratedCatalogViewDto>;
