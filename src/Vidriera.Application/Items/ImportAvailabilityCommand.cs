using MediatR;

namespace Vidriera.Application.Items;

public record ImportAvailabilityCommand(Guid CompanyId, Stream FileContent) : IRequest<ImportAvailabilityResult>;

public record ImportAvailabilityResult(int MarkedOutOfStockCount, int MarkedInStockCount, IReadOnlyList<string> NotFoundCodes);
