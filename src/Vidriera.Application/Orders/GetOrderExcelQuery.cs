using MediatR;

namespace Vidriera.Application.Orders;

public record GetOrderExcelQuery(Guid CompanyId, Guid OrderId) : IRequest<OrderExcelResult>;
