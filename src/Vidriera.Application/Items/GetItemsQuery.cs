using MediatR;

namespace Vidriera.Application.Items;

public record GetItemsQuery(Guid CompanyId) : IRequest<IReadOnlyList<ItemDto>>;
