using MediatR;

namespace Vidriera.Application.Catalogs;

public record GetCatalogPageQuery(Guid CatalogId, int PageNumber) : IRequest<CatalogPageResult>;

public record CatalogPageResult(Stream Content, string ContentType);
