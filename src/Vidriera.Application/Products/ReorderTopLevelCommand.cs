using MediatR;
using Vidriera.Application.Common;

namespace Vidriera.Application.Products;

public record ReorderTopLevelCommand(Guid CompanyId, IReadOnlyList<OrderedItemRef> OrderedItems) : IRequest;
