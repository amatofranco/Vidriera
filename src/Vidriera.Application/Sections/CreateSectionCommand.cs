using MediatR;

namespace Vidriera.Application.Sections;

public record CreateSectionCommand(
    Guid CompanyId,
    Stream? FileContent,
    string? OriginalFileName,
    string? Name,
    Guid? ParentSectionId) : IRequest<SectionDto>;
