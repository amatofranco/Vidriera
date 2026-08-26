using MediatR;
using NHibernate;
using NHibernate.Linq;
using Vidriera.Application.Common;
using Vidriera.Domain.Entities;

namespace Vidriera.Application.Orders;

public class DeleteOrderFormFieldCommandHandler : IRequestHandler<DeleteOrderFormFieldCommand>
{
    private readonly ISession _session;

    public DeleteOrderFormFieldCommandHandler(ISession session)
    {
        _session = session;
    }

    public async Task Handle(DeleteOrderFormFieldCommand request, CancellationToken cancellationToken)
    {
        var field = await _session.Query<OrderFormField>()
            .GetOrThrowAsync(
                f => f.Id == request.FieldId && f.Company.Id == request.CompanyId,
                ErrorMessages.OrderFormFieldNotFound(request.FieldId),
                cancellationToken);

        await _session.DeleteInTransactionAsync(field, cancellationToken);
    }
}
