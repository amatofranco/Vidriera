using MediatR;

namespace Vidriera.Application.Orders;

public record OrderItemDto(string ItemName, string? Code, int Quantity);

public record OrderDto(
    Guid Id,
    DateTime CreatedAt,
    IReadOnlyList<CustomerFieldSnapshotEntry> CustomerFields,
    IReadOnlyList<OrderItemDto> Items);

public record GetOrdersQuery(Guid CompanyId) : IRequest<IReadOnlyList<OrderDto>>;
