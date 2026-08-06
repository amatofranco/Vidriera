using MediatR;
using NHibernate;
using NHibernate.Linq;
using Vidriera.Application.Common;
using Vidriera.Application.Common.Exceptions;
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

        Section? parent = null;
        if (request.ParentSectionId.HasValue)
        {
            if (request.ParentSectionId.Value == section.Id)
            {
                throw new ValidationException(ErrorMessages.SectionCannotBeOwnParent);
            }

            parent = await _session.Query<Section>().GetOrThrowAsync(
                s => s.Id == request.ParentSectionId.Value && s.Company.Id == request.CompanyId,
                ErrorMessages.SectionNotFound(request.ParentSectionId.Value),
                cancellationToken);

            if (parent.ParentSection is not null)
            {
                throw new ValidationException(ErrorMessages.SectionCannotNestFurther);
            }

            var hasOwnChildren = await _session.Query<Section>()
                .AnyAsync(s => s.ParentSection != null && s.ParentSection.Id == section.Id, cancellationToken);
            if (hasOwnChildren)
            {
                throw new ValidationException(ErrorMessages.SectionHasChildrenCannotNest);
            }
        }

        var nextSortOrder = parent is null
            ? await TopLevelOrdering.NextTopLevelSortOrderAsync(_session, request.CompanyId, cancellationToken)
            : await TopLevelOrdering.NextSectionSortOrderAsync(_session, parent.Id, cancellationToken);

        section.ParentSection = parent;
        section.SortOrder = nextSortOrder;

        await _session.UpdateInTransactionAsync(section, cancellationToken);
    }
}
