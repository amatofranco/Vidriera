using MediatR;
using NHibernate;
using NHibernate.Linq;
using Vidriera.Application.Common.Exceptions;
using Vidriera.Domain.Entities;

namespace Vidriera.Application.Products;

public class ReorderProductsCommandHandler : IRequestHandler<ReorderProductsCommand>
{
    private readonly ISession _session;

    public ReorderProductsCommandHandler(ISession session)
    {
        _session = session;
    }

    public async Task Handle(ReorderProductsCommand request, CancellationToken cancellationToken)
    {
        var products = await _session.Query<Product>()
            .Where(p => p.Company.Id == request.CompanyId && request.OrderedProductIds.Contains(p.Id))
            .ToListAsync(cancellationToken);

        var productsById = products.ToDictionary(p => p.Id);

        if (productsById.Count != request.OrderedProductIds.Count)
        {
            throw new ValidationException("Alguno de los productos no existe o no pertenece a esta empresa.");
        }

        using var transaction = _session.BeginTransaction();

        for (var i = 0; i < request.OrderedProductIds.Count; i++)
        {
            var product = productsById[request.OrderedProductIds[i]];
            product.SortOrder = i;
            await _session.UpdateAsync(product, cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }
}
