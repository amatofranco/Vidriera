using MediatR;

namespace Vidriera.Application.Products;

public record ReorderProductsCommand(Guid CompanyId, IReadOnlyList<Guid> OrderedProductIds) : IRequest;
