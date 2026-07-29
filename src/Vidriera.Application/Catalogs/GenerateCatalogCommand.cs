using MediatR;

namespace Vidriera.Application.Catalogs;

public record GenerateCatalogCommand(Guid CompanyId, Guid UserId, IReadOnlyList<Guid> ProductIds) : IRequest<GenerateCatalogResult>;

public record GenerateCatalogResult(Guid Id, string Url, DateTime? ExpiresAt);
