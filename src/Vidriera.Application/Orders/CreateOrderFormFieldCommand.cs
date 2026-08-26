using MediatR;

namespace Vidriera.Application.Orders;

public record CreateOrderFormFieldCommand(Guid CompanyId, string Label, string FieldType, bool IsRequired) : IRequest<OrderFormFieldDto>;
