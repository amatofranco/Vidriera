using MediatR;
using NHibernate;
using Vidriera.Application.Common;
using Vidriera.Domain.Entities;

namespace Vidriera.Application.Items;

public class AssignItemSectionCommandHandler : IRequestHandler<AssignItemSectionCommand>
{
    private readonly ISession _session;

    public AssignItemSectionCommandHandler(ISession session)
    {
        _session = session;
    }

    public async Task Handle(AssignItemSectionCommand request, CancellationToken cancellationToken)
    {
        var item = await _session.Query<Item>().GetOrThrowAsync(
            p => p.Id == request.ItemId && p.Company.Id == request.CompanyId,
            ErrorMessages.ItemNotFound(request.ItemId),
            cancellationToken);

        Section? section = null;
        if (request.SectionId.HasValue)
        {
            section = await _session.Query<Section>().GetOrThrowAsync(
                s => s.Id == request.SectionId.Value && s.Company.Id == request.CompanyId,
                ErrorMessages.SectionNotFound(request.SectionId.Value),
                cancellationToken);
        }

        var nextSortOrder = section is null
            ? await TopLevelOrdering.NextTopLevelSortOrderAsync(_session, request.CompanyId, cancellationToken)
            : await TopLevelOrdering.NextSectionSortOrderAsync(_session, section.Id, cancellationToken);

        item.Section = section;
        item.SortOrder = nextSortOrder;

        await _session.UpdateInTransactionAsync(item, cancellationToken);
    }
}
