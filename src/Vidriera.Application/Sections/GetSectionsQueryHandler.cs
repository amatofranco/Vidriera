using MediatR;
using NHibernate;
using NHibernate.Linq;
using Vidriera.Domain.Entities;

namespace Vidriera.Application.Sections;

public class GetSectionsQueryHandler : IRequestHandler<GetSectionsQuery, IReadOnlyList<SectionDto>>
{
    private readonly ISession _session;

    public GetSectionsQueryHandler(ISession session)
    {
        _session = session;
    }

    public async Task<IReadOnlyList<SectionDto>> Handle(GetSectionsQuery request, CancellationToken cancellationToken)
    {
        var sections = await _session.Query<Section>()
            .Where(s => s.Company.Id == request.CompanyId)
            .OrderBy(s => s.SortOrder)
            .ToListAsync(cancellationToken);

        return sections
            .Select(s => new SectionDto(s.Id, s.Name, s.SortOrder, s.ParentSection?.Id))
            .ToList();
    }
}
