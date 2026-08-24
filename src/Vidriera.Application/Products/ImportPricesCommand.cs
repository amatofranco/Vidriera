using MediatR;

namespace Vidriera.Application.Products;

public record ImportPricesCommand(Guid CompanyId, Stream FileContent) : IRequest<ImportPricesResult>;

public record ImportPricesResult(int UpdatedCount, IReadOnlyList<string> NotFoundCodes);
