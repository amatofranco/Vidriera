using MediatR;

namespace Vidriera.Application.Items;

public record ImportPricesCommand(Guid CompanyId, Stream FileContent) : IRequest<ImportPricesResult>;

public record ImportPricesResult(int UpdatedCount, IReadOnlyList<string> NotFoundCodes);
