using MediatR;

namespace Vidriera.Application.Items;

public record UpdateStockCommand(Guid CompanyId, Guid ItemId, bool HasStock) : IRequest;
