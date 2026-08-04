using MediatR;
using NHibernate;
using Vidriera.Application.Common;
using Vidriera.Domain.Entities;

namespace Vidriera.Application.Products;

public class UpdateStockCommandHandler : IRequestHandler<UpdateStockCommand>
{
    private readonly ISession _session;

    public UpdateStockCommandHandler(ISession session)
    {
        _session = session;
    }

    public async Task Handle(UpdateStockCommand request, CancellationToken cancellationToken)
    {
        var product = await _session.Query<Product>().GetOrThrowAsync(
            p => p.Id == request.ProductId && p.Company.Id == request.CompanyId,
            ErrorMessages.ProductNotFound(request.ProductId),
            cancellationToken);

        product.HasStock = request.HasStock;

        await _session.UpdateInTransactionAsync(product, cancellationToken);
    }
}
