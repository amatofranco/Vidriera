using MediatR;

namespace Vidriera.Application.Orders;

public record ReorderOrderFormFieldsCommand(Guid CompanyId, IReadOnlyList<Guid> OrderedFieldIds) : IRequest;
