using MediatR;

namespace Vidriera.Application.Items;

public record AssignItemSectionCommand(Guid CompanyId, Guid ItemId, Guid? SectionId) : IRequest;
