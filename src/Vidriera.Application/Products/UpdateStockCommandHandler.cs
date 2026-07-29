using MediatR;
using NHibernate;
using NHibernate.Linq;
using Vidriera.Application.Common.Exceptions;
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
        var product = await _session.Query<Product>()
            .FirstOrDefaultAsync(p => p.Id == request.ProductId && p.Company.Id == request.CompanyId, cancellationToken);

        if (product is null)
        {
            throw new NotFoundException($"No se encontró el producto {request.ProductId} para esta empresa.");
        }

        product.HasStock = request.HasStock;

        using var transaction = _session.BeginTransaction();
        await _session.UpdateAsync(product, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
