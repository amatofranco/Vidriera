using MediatR;

namespace Vidriera.Application.Products;

public record UpdatePriceCommand(Guid CompanyId, Guid ProductId, decimal? Price) : IRequest;
