using MediatR;

namespace Vidriera.Application.Products;

public record ReorderSectionProductsCommand(Guid CompanyId, Guid SectionId, IReadOnlyList<Guid> OrderedProductIds) : IRequest;
