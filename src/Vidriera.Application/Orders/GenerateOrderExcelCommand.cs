using MediatR;

namespace Vidriera.Application.Orders;

public record OrderLineItem(Guid ProductId, int Quantity);

public record CustomerOrderInfo(
    string BusinessName,
    string StoreName,
    string Cuit,
    string VatCondition,
    string Phone,
    string Email,
    string City,
    string Province,
    string? Carrier,
    string DeliveryAddress);

public record GenerateOrderExcelCommand(
    Guid CompanyId,
    IReadOnlyList<OrderLineItem> Items,
    CustomerOrderInfo Customer) : IRequest<OrderExcelResult>;

public record OrderExcelResult(byte[] Content, string FileName);
