using MediatR;

namespace Vidriera.Application.Orders;

public record UpdateOrderFormFieldCommand(Guid CompanyId, Guid FieldId, string Label, string FieldType, bool IsRequired) : IRequest;
