using MediatR;
using NHibernate;
using NHibernate.Linq;
using Vidriera.Application.Common;
using Vidriera.Application.Common.Exceptions;
using Vidriera.Domain.Entities;

namespace Vidriera.Application.Orders;

public class UpdateOrderFormFieldCommandHandler : IRequestHandler<UpdateOrderFormFieldCommand>
{
    private readonly ISession _session;

    public UpdateOrderFormFieldCommandHandler(ISession session)
    {
        _session = session;
    }

    public async Task Handle(UpdateOrderFormFieldCommand request, CancellationToken cancellationToken)
    {
        var label = request.Label.Trim();
        if (string.IsNullOrWhiteSpace(label))
        {
            throw new ValidationException(ErrorMessages.OrderFormFieldLabelRequired);
        }

        if (!OrderFieldTypes.IsValid(request.FieldType))
        {
            throw new ValidationException(ErrorMessages.InvalidOrderFieldType);
        }

        var field = await _session.Query<OrderFormField>()
            .GetOrThrowAsync(
                f => f.Id == request.FieldId && f.Company.Id == request.CompanyId,
                ErrorMessages.OrderFormFieldNotFound(request.FieldId),
                cancellationToken);

        field.Label = label;
        field.FieldType = request.FieldType;
        field.IsRequired = request.IsRequired;

        await _session.UpdateInTransactionAsync(field, cancellationToken);
    }
}
