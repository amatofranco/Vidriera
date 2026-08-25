using MediatR;
using NHibernate;
using Vidriera.Application.Common;
using Vidriera.Domain.Entities;

namespace Vidriera.Application.Items;

public class UpdateStockCommandHandler : IRequestHandler<UpdateStockCommand>
{
    private readonly ISession _session;

    public UpdateStockCommandHandler(ISession session)
    {
        _session = session;
    }

    public async Task Handle(UpdateStockCommand request, CancellationToken cancellationToken)
    {
        var item = await _session.Query<Item>().GetOrThrowAsync(
            p => p.Id == request.ItemId && p.Company.Id == request.CompanyId,
            ErrorMessages.ItemNotFound(request.ItemId),
            cancellationToken);

        item.HasStock = request.HasStock;

        await _session.UpdateInTransactionAsync(item, cancellationToken);
    }
}
