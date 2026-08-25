using NHibernate;
using NHibernate.Linq;
using Vidriera.Application.Common.Exceptions;
using Vidriera.Domain.Entities;

namespace Vidriera.Application.Common;

public record OrderedItemRef(bool IsSection, Guid Id);

internal static class ReorderApplier
{
    public static async Task ApplyAsync(
        ISession session,
        IReadOnlyList<OrderedItemRef> orderedItems,
        IQueryable<Section> sectionScope,
        IQueryable<Item> itemScope,
        string invalidItemsMessage,
        CancellationToken cancellationToken)
    {
        var sectionIds = orderedItems.Where(i => i.IsSection).Select(i => i.Id).ToList();
        var itemIds = orderedItems.Where(i => !i.IsSection).Select(i => i.Id).ToList();

        var sections = await sectionScope.Where(s => sectionIds.Contains(s.Id)).ToListAsync(cancellationToken);
        var items = await itemScope.Where(p => itemIds.Contains(p.Id)).ToListAsync(cancellationToken);

        if (sections.Count != sectionIds.Count || items.Count != itemIds.Count)
        {
            throw new ValidationException(invalidItemsMessage);
        }

        var sectionsById = sections.ToDictionary(s => s.Id);
        var itemsById = items.ToDictionary(p => p.Id);

        using var transaction = session.BeginTransaction();

        for (var i = 0; i < orderedItems.Count; i++)
        {
            var item = orderedItems[i];
            if (item.IsSection)
            {
                var section = sectionsById[item.Id];
                section.SortOrder = i;
                await session.UpdateAsync(section, cancellationToken);
            }
            else
            {
                var itemEntity = itemsById[item.Id];
                itemEntity.SortOrder = i;
                await session.UpdateAsync(itemEntity, cancellationToken);
            }
        }

        await transaction.CommitAsync(cancellationToken);
    }
}
