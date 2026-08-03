using MediatR;

namespace Vidriera.Application.Products;

public record TopLevelItemRef(bool IsSection, Guid Id);

public record ReorderTopLevelCommand(Guid CompanyId, IReadOnlyList<TopLevelItemRef> OrderedItems) : IRequest;
