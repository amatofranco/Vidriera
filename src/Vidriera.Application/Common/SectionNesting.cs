using NHibernate;
using NHibernate.Linq;
using Vidriera.Application.Common.Exceptions;
using Vidriera.Domain.Entities;

namespace Vidriera.Application.Common;

internal static class SectionNesting
{
    public static async Task<Section?> ResolveParentAsync(
        ISession session,
        Guid companyId,
        Guid? parentSectionId,
        Guid? excludeSectionId,
        CancellationToken cancellationToken)
    {
        if (!parentSectionId.HasValue)
        {
            return null;
        }

        if (excludeSectionId.HasValue && parentSectionId.Value == excludeSectionId.Value)
        {
            throw new ValidationException(ErrorMessages.SectionCannotBeOwnParent);
        }

        var parent = await session.Query<Section>().GetOrThrowAsync(
            s => s.Id == parentSectionId.Value && s.Company.Id == companyId,
            ErrorMessages.SectionNotFound(parentSectionId.Value),
            cancellationToken);

        if (parent.ParentSection is not null)
        {
            throw new ValidationException(ErrorMessages.SectionCannotNestFurther);
        }

        if (excludeSectionId.HasValue)
        {
            var hasOwnChildren = await session.Query<Section>()
                .AnyAsync(s => s.ParentSection != null && s.ParentSection.Id == excludeSectionId.Value, cancellationToken);
            if (hasOwnChildren)
            {
                throw new ValidationException(ErrorMessages.SectionHasChildrenCannotNest);
            }
        }

        return parent;
    }
}
