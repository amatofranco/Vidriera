using MediatR;

namespace Vidriera.Application.Products;

public record DeleteProductCommand(Guid CompanyId, Guid ProductId) : IRequest;
