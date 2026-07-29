using MediatR;

namespace Vidriera.Application.Products;

public record UpdateStockCommand(Guid CompanyId, Guid ProductId, bool HasStock) : IRequest;
