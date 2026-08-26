using MediatR;
using NHibernate;
using NHibernate.Linq;
using Vidriera.Application.Common;
using Vidriera.Application.Common.Exceptions;
using Vidriera.Domain.Entities;

namespace Vidriera.Application.Orders;

public class ReorderOrderFormFieldsCommandHandler : IRequestHandler<ReorderOrderFormFieldsCommand>
{
    private readonly ISession _session;

    public ReorderOrderFormFieldsCommandHandler(ISession session)
    {
        _session = session;
    }

    public async Task Handle(ReorderOrderFormFieldsCommand request, CancellationToken cancellationToken)
    {
        var fields = await _session.Query<OrderFormField>()
            .Where(f => f.Company.Id == request.CompanyId && request.OrderedFieldIds.Contains(f.Id))
            .ToListAsync(cancellationToken);

        if (fields.Count != request.OrderedFieldIds.Count)
        {
            throw new ValidationException(ErrorMessages.InvalidOrderFormFieldReorderItems);
        }

        var fieldsById = fields.ToDictionary(f => f.Id);

        using var transaction = _session.BeginTransaction();

        for (var i = 0; i < request.OrderedFieldIds.Count; i++)
        {
            var field = fieldsById[request.OrderedFieldIds[i]];
            field.SortOrder = i;
            await _session.UpdateAsync(field, cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }
}
