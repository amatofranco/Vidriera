using MediatR;

namespace Vidriera.Application.Items;

public record UpdateCodeCommand(Guid CompanyId, Guid ItemId, string? Code) : IRequest;
