using MediatR;
using NHibernate;
using Vidriera.Application.Common;
using Vidriera.Domain.Entities;

namespace Vidriera.Application.Sections;

public class AssignSectionParentCommandHandler : IRequestHandler<AssignSectionParentCommand>
{
    private readonly ISession _session;

    public AssignSectionParentCommandHandler(ISession session)
    {
        _session = session;
    }

    public async Task Handle(AssignSectionParentCommand request, CancellationToken cancellationToken)
    {
        var section = await _session.Query<Section>().GetOrThrowAsync(
            s => s.Id == request.SectionId && s.Company.Id == request.CompanyId,
            ErrorMessages.SectionNotFound(request.SectionId),
            cancellationToken);

        var parent = await SectionNesting.ResolveParentAsync(_session, request.CompanyId, request.ParentSectionId, section.Id, cancellationToken);

        var nextSortOrder = parent is null
            ? await TopLevelOrdering.NextTopLevelSortOrderAsync(_session, request.CompanyId, cancellationToken)
            : await TopLevelOrdering.NextSectionSortOrderAsync(_session, parent.Id, cancellationToken);

        section.ParentSection = parent;
        section.SortOrder = nextSortOrder;

        await _session.UpdateInTransactionAsync(section, cancellationToken);
    }
}
