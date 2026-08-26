using MediatR;
using NHibernate;
using NHibernate.Linq;
using Vidriera.Application.Common;
using Vidriera.Application.Common.Exceptions;
using Vidriera.Domain.Entities;

namespace Vidriera.Application.Orders;

public class CreateOrderFormFieldCommandHandler : IRequestHandler<CreateOrderFormFieldCommand, OrderFormFieldDto>
{
    private readonly ISession _session;

    public CreateOrderFormFieldCommandHandler(ISession session)
    {
        _session = session;
    }

    public async Task<OrderFormFieldDto> Handle(CreateOrderFormFieldCommand request, CancellationToken cancellationToken)
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

        var company = await _session.GetOrThrowAsync<Company>(
            request.CompanyId,
            ErrorMessages.CompanyNotFound(request.CompanyId),
            cancellationToken);

        var maxSortOrder = await _session.Query<OrderFormField>()
            .Where(f => f.Company.Id == request.CompanyId)
            .Select(f => (int?)f.SortOrder)
            .MaxAsync(cancellationToken);

        var field = new OrderFormField
        {
            Company = company,
            Label = label,
            FieldType = request.FieldType,
            IsRequired = request.IsRequired,
            SortOrder = (maxSortOrder ?? -1) + 1,
        };

        await _session.SaveInTransactionAsync(field, cancellationToken);

        return new OrderFormFieldDto(field.Id, field.Label, field.FieldType, field.IsRequired, field.SortOrder);
    }
}
