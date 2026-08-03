using MediatR;

namespace Vidriera.Application.Sections;

public record DeleteSectionCommand(Guid CompanyId, Guid SectionId) : IRequest;
