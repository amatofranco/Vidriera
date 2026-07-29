using MediatR;

namespace Vidriera.Application.Catalogs;

public record GetCatalogFileQuery(Guid Id) : IRequest<CatalogFileResult>;

public record CatalogFileResult(Stream Content, string ContentType, string FileName);
