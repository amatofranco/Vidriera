using MediatR;

namespace Vidriera.Application.Items;

public record UpdateNameCommand(Guid CompanyId, Guid ItemId, string Name) : IRequest;
