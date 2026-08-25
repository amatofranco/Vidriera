using MediatR;
using Vidriera.Application.Common;

namespace Vidriera.Application.Items;

public record ReorderTopLevelCommand(Guid CompanyId, IReadOnlyList<OrderedItemRef> OrderedItems) : IRequest;
