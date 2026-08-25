using MediatR;

namespace Vidriera.Application.Items;

public record DeleteItemCommand(Guid CompanyId, Guid ItemId) : IRequest;
