using MediatR;
using NHibernate;
using Vidriera.Application.Common;
using Vidriera.Domain.Entities;

namespace Vidriera.Application.Items;

public class UpdateCodeCommandHandler : IRequestHandler<UpdateCodeCommand>
{
    private readonly ISession _session;

    public UpdateCodeCommandHandler(ISession session)
    {
        _session = session;
    }

    public async Task Handle(UpdateCodeCommand request, CancellationToken cancellationToken)
    {
        var item = await _session.Query<Item>().GetOrThrowAsync(
            p => p.Id == request.ItemId && p.Company.Id == request.CompanyId,
            ErrorMessages.ItemNotFound(request.ItemId),
            cancellationToken);

        item.Code = string.IsNullOrWhiteSpace(request.Code) ? null : request.Code.Trim();

        await _session.UpdateInTransactionAsync(item, cancellationToken);
    }
}
