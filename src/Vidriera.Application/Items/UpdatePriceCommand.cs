using MediatR;

namespace Vidriera.Application.Items;

public record UpdatePriceCommand(Guid CompanyId, Guid ItemId, decimal? Price) : IRequest;
