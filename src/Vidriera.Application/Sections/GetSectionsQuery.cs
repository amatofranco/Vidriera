using MediatR;

namespace Vidriera.Application.Sections;

public record GetSectionsQuery(Guid CompanyId) : IRequest<IReadOnlyList<SectionDto>>;
