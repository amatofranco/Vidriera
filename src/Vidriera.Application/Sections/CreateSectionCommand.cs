using MediatR;

namespace Vidriera.Application.Sections;

public record CreateSectionCommand(
    Guid CompanyId,
    Stream FileContent,
    string OriginalFileName,
    string? Name) : IRequest<SectionDto>;
