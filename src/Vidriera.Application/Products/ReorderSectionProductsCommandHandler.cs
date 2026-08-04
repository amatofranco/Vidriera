using MediatR;
using NHibernate;
using NHibernate.Linq;
using Vidriera.Application.Common;
using Vidriera.Application.Common.Exceptions;
using Vidriera.Domain.Entities;

namespace Vidriera.Application.Products;

public class ReorderSectionProductsCommandHandler : IRequestHandler<ReorderSectionProductsCommand>
{
    private readonly ISession _session;

    public ReorderSectionProductsCommandHandler(ISession session)
    {
        _session = session;
    }

    public async Task Handle(ReorderSectionProductsCommand request, CancellationToken cancellationToken)
    {
        var products = await _session.Query<Product>()
            .Where(p => p.Company.Id == request.CompanyId
                && p.Section != null && p.Section.Id == request.SectionId
                && request.OrderedProductIds.Contains(p.Id))
            .ToListAsync(cancellationToken);

        var productsById = products.ToDictionary(p => p.Id);

        if (productsById.Count != request.OrderedProductIds.Count)
        {
            throw new ValidationException(ErrorMessages.InvalidSectionReorderItems);
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
