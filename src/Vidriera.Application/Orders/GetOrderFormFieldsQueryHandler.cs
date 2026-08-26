using MediatR;
using NHibernate;
using NHibernate.Linq;
using Vidriera.Domain.Entities;

namespace Vidriera.Application.Orders;

public class GetOrderFormFieldsQueryHandler : IRequestHandler<GetOrderFormFieldsQuery, IReadOnlyList<OrderFormFieldDto>>
{
    private readonly ISession _session;

    public GetOrderFormFieldsQueryHandler(ISession session)
    {
        _session = session;
    }

    public async Task<IReadOnlyList<OrderFormFieldDto>> Handle(GetOrderFormFieldsQuery request, CancellationToken cancellationToken)
    {
        return await _session.Query<OrderFormField>()
            .Where(f => f.Company.Id == request.CompanyId)
            .OrderBy(f => f.SortOrder)
            .Select(f => new OrderFormFieldDto(f.Id, f.Label, f.FieldType, f.IsRequired, f.SortOrder))
            .ToListAsync(cancellationToken);
    }
}
