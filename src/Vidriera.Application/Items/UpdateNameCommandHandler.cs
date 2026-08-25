using MediatR;
using NHibernate;
using Vidriera.Application.Common;
using Vidriera.Application.Common.Exceptions;
using Vidriera.Domain.Entities;

namespace Vidriera.Application.Items;

public class UpdateNameCommandHandler : IRequestHandler<UpdateNameCommand>
{
    private readonly ISession _session;

    public UpdateNameCommandHandler(ISession session)
    {
        _session = session;
    }

    public async Task Handle(UpdateNameCommand request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        if (string.IsNullOrEmpty(name))
        {
            throw new ValidationException(ErrorMessages.ItemNameRequired);
        }

        var item = await _session.Query<Item>().GetOrThrowAsync(
            p => p.Id == request.ItemId && p.Company.Id == request.CompanyId,
            ErrorMessages.ItemNotFound(request.ItemId),
            cancellationToken);

        item.Name = name;

        await _session.UpdateInTransactionAsync(item, cancellationToken);
    }
}
