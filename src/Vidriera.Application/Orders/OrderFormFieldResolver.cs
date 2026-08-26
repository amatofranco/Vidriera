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
            .ToListAsync(cancellationToken);

        if (fields.Count == 0)
        {
            fields = await SeedDefaultsAsync(session, companyId, cancellationToken);
        }

        return fields
            .Select(f => new OrderFormFieldDto(f.Id, f.Label, f.FieldType, f.IsRequired, f.SortOrder))
            .ToList();
    }

    private static async Task<List<OrderFormField>> SeedDefaultsAsync(ISession session, Guid companyId, CancellationToken cancellationToken)
    {
        var company = session.Load<Company>(companyId);

        var name = new OrderFormField { Company = company, Label = "Nombre", FieldType = OrderFieldTypes.Name, IsRequired = true, SortOrder = 0 };
        var email = new OrderFormField { Company = company, Label = "Email", FieldType = OrderFieldTypes.Email, IsRequired = true, SortOrder = 1 };

        using var transaction = session.BeginTransaction();
        await session.SaveAsync(name, cancellationToken);
        await session.SaveAsync(email, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return [name, email];
    }
}
