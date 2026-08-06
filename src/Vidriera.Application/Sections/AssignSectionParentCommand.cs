using MediatR;

namespace Vidriera.Application.Sections;

public record AssignSectionParentCommand(Guid CompanyId, Guid SectionId, Guid? ParentSectionId) : IRequest;
