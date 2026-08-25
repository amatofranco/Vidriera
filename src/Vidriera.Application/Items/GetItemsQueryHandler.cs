using MediatR;
using NHibernate;
using NHibernate.Linq;
using Vidriera.Domain.Entities;

namespace Vidriera.Application.Items;

public class GetItemsQueryHandler : IRequestHandler<GetItemsQuery, IReadOnlyList<ItemDto>>
{
    private readonly ISession _session;

    public GetItemsQueryHandler(ISession session)
    {
        _session = session;
    }

    public async Task<IReadOnlyList<ItemDto>> Handle(GetItemsQuery request, CancellationToken cancellationToken)
    {
        var items = await _session.Query<Item>()
            .Where(p => p.Company.Id == request.CompanyId && p.IsActive)
            .OrderBy(p => p.SortOrder)
            .ToListAsync(cancellationToken);

        return items
            .Select(p => new ItemDto(p.Id, p.Name, p.HasStock, !string.IsNullOrEmpty(p.SheetPdfBlobKey), p.Section?.Id, p.SortOrder, p.Code, p.Price))
            .ToList();
    }
}
