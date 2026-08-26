using NHibernate;
using NHibernate.Linq;
using Vidriera.Domain.Entities;

namespace Vidriera.Application.Orders;

public static class OrderFormFieldResolver
{
    public static async Task<IReadOnlyList<OrderFormFieldDto>> ResolveAsync(
        ISession session,
        Guid companyId,
        CancellationToken cancellationToken)
    {
        var fields = await session.Query<OrderFormField>()
            .Where(f => f.Company.Id == companyId)
            .OrderBy(f => f.SortOrder)
            .Select(f => new OrderFormFieldDto(f.Id, f.Label, f.FieldType, f.IsRequired, f.SortOrder))
            .ToListAsync(cancellationToken);

        return fields.Count > 0 ? fields : DefaultOrderFormFields.Fields;
    }
}
