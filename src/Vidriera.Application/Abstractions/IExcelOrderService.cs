using Vidriera.Application.Orders;

namespace Vidriera.Application.Abstractions;

public record OrderExcelLine(string ProductName, string? Code, int Quantity);

public interface IExcelOrderService
{
    byte[] GenerateOrderWorkbook(string companyName, CustomerOrderInfo customer, IReadOnlyList<OrderExcelLine> lines);
}
