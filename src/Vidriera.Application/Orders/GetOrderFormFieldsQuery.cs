using MediatR;

namespace Vidriera.Application.Orders;

public record GetOrderFormFieldsQuery(Guid CompanyId) : IRequest<IReadOnlyList<OrderFormFieldDto>>;
