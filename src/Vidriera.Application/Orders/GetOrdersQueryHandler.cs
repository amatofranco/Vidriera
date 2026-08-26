using System.Text.Json;
using MediatR;
using NHibernate;
using NHibernate.Linq;
using Vidriera.Application.Abstractions;
using Vidriera.Domain.Entities;

namespace Vidriera.Application.Orders;

public class GetOrdersQueryHandler : IRequestHandler<GetOrdersQuery, IReadOnlyList<OrderDto>>
{
    private readonly ISession _session;

    public GetOrdersQueryHandler(ISession session)
    {
        _session = session;
    }

    public async Task<IReadOnlyList<OrderDto>> Handle(GetOrdersQuery request, CancellationToken cancellationToken)
    {
        var orders = await _session.Query<Order>()
            .Where(o => o.Company.Id == request.CompanyId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(cancellationToken);

        return orders.Select(ToDto).ToList();
    }

    private static OrderDto ToDto(Order order)
    {
        var lines = JsonSerializer.Deserialize<List<OrderExcelLine>>(order.ItemsSnapshotJson) ?? [];
        var items = lines.Select(l => new OrderItemDto(l.ItemName, l.Code, l.Quantity)).ToList();
        var customerFields = OrderCustomerFieldsResolver.Resolve(order);

        return new OrderDto(order.Id, order.CreatedAt, customerFields, items);
    }
}
