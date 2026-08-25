using MediatR;
using NHibernate;
using Vidriera.Application.Common;
using Vidriera.Domain.Entities;

namespace Vidriera.Application.Items;

public class UpdatePriceCommandHandler : IRequestHandler<UpdatePriceCommand>
{
    private readonly ISession _session;

    public UpdatePriceCommandHandler(ISession session)
    {
        _session = session;
    }

    public async Task Handle(UpdatePriceCommand request, CancellationToken cancellationToken)
    {
        var item = await _session.Query<Item>().GetOrThrowAsync(
            p => p.Id == request.ItemId && p.Company.Id == request.CompanyId,
            ErrorMessages.ItemNotFound(request.ItemId),
            cancellationToken);

        item.Price = request.Price;

        await _session.UpdateInTransactionAsync(item, cancellationToken);
    }
}
