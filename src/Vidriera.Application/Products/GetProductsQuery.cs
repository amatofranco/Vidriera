using MediatR;

namespace Vidriera.Application.Products;

public record GetProductsQuery(Guid CompanyId) : IRequest<IReadOnlyList<ProductDto>>;
