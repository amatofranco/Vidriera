using MediatR;

namespace Vidriera.Application.Items;

public record CreateItemCommand(
    Guid CompanyId,
    Stream FileContent,
    string OriginalFileName,
    string? Name,
    string? Code,
    decimal? Price = null) : IRequest<ItemDto>;
