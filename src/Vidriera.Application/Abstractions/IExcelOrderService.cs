using Vidriera.Application.Orders;

namespace Vidriera.Application.Abstractions;

public record OrderExcelLine(string ItemName, string? Code, int Quantity, decimal? UnitPrice = null);

public interface IExcelOrderService
{
    byte[] GenerateOrderWorkbook(string companyName, CustomerOrderInfo customer, IReadOnlyList<OrderExcelLine> lines);
}
