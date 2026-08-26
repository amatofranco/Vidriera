using MediatR;
using NHibernate;

namespace Vidriera.Application.Orders;

public class GetOrderFormFieldsQueryHandler : IRequestHandler<GetOrderFormFieldsQuery, IReadOnlyList<OrderFormFieldDto>>
{
    private readonly ISession _session;

    public GetOrderFormFieldsQueryHandler(ISession session)
    {
        _session = session;
    }

    public Task<IReadOnlyList<OrderFormFieldDto>> Handle(GetOrderFormFieldsQuery request, CancellationToken cancellationToken)
        => OrderFormFieldResolver.ResolveAsync(_session, request.CompanyId, cancellationToken);
}
