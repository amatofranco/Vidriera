using MediatR;
using Vidriera.Application.Common;

namespace Vidriera.Application.Products;

public record ReorderSectionChildrenCommand(Guid CompanyId, Guid ParentSectionId, IReadOnlyList<OrderedItemRef> OrderedItems) : IRequest;
