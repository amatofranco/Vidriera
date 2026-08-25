using MediatR;
using Vidriera.Application.Abstractions;

namespace Vidriera.Application.Subscriptions;

public class GetPreapprovalDetailsQueryHandler : IRequestHandler<GetPreapprovalDetailsQuery, MercadoPagoPreapproval>
{
    private readonly IMercadoPagoClient _mercadoPagoClient;

    public GetPreapprovalDetailsQueryHandler(IMercadoPagoClient mercadoPagoClient)
    {
        _mercadoPagoClient = mercadoPagoClient;
    }

    public Task<MercadoPagoPreapproval> Handle(GetPreapprovalDetailsQuery request, CancellationToken cancellationToken) =>
        _mercadoPagoClient.GetPreapprovalAsync(request.PreapprovalId, cancellationToken);
}
