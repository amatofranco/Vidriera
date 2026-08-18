using MediatR;

namespace Vidriera.Application.Orders;

public record OrderItemDto(string ProductName, string? Isbn, int Quantity);

public record OrderDto(
    Guid Id,
    DateTime CreatedAt,
    string BusinessName,
    string? StoreName,
    string Cuit,
    string? VatCondition,
    string? Phone,
    string Email,
    string? City,
    string? Province,
    string? Carrier,
    string? DeliveryAddress,
    IReadOnlyList<OrderItemDto> Items);

public record GetOrdersQuery(Guid CompanyId) : IRequest<IReadOnlyList<OrderDto>>;
