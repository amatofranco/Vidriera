using System.Text.Json;
using MediatR;
using NHibernate;
using NHibernate.Linq;
using Vidriera.Application.Abstractions;
using Vidriera.Application.Common;
using Vidriera.Application.Common.Exceptions;
using Vidriera.Domain.Entities;

namespace Vidriera.Application.Orders;

public class GetOrderExcelQueryHandler : IRequestHandler<GetOrderExcelQuery, OrderExcelResult>
{
    private readonly ISession _session;
    private readonly IExcelOrderService _excelOrderService;

    public GetOrderExcelQueryHandler(ISession session, IExcelOrderService excelOrderService)
    {
        _session = session;
        _excelOrderService = excelOrderService;
    }

    public async Task<OrderExcelResult> Handle(GetOrderExcelQuery request, CancellationToken cancellationToken)
    {
        var order = await _session.Query<Order>()
            .FirstOrDefaultAsync(o => o.Id == request.OrderId && o.Company.Id == request.CompanyId, cancellationToken);

        if (order is null)
        {
            throw new NotFoundException(ErrorMessages.OrderNotFound(request.OrderId));
        }

        var lines = JsonSerializer.Deserialize<List<OrderExcelLine>>(order.ItemsSnapshotJson) ?? [];
        var customerFields = OrderCustomerFieldsResolver.Resolve(order);

        var content = _excelOrderService.GenerateOrderWorkbook(order.Company.Name, customerFields, lines);
        var fileName = OrderExcelFileName.Build(order.CreatedAt);

        return new OrderExcelResult(content, fileName);
    }
}
