using MediatR;
using NHibernate;
using NHibernate.Linq;
using Vidriera.Application.Common;
using Vidriera.Application.Common.Exceptions;
using Vidriera.Domain.Entities;

namespace Vidriera.Application.Products;

public class ReorderTopLevelCommandHandler : IRequestHandler<ReorderTopLevelCommand>
{
    private readonly ISession _session;

    public ReorderTopLevelCommandHandler(ISession session)
    {
        _session = session;
    }

    public async Task Handle(ReorderTopLevelCommand request, CancellationToken cancellationToken)
    {
        var sectionIds = request.OrderedItems.Where(i => i.IsSection).Select(i => i.Id).ToList();
        var productIds = request.OrderedItems.Where(i => !i.IsSection).Select(i => i.Id).ToList();

        var sections = await _session.Query<Section>()
            .Where(s => s.Company.Id == request.CompanyId && sectionIds.Contains(s.Id))
            .ToListAsync(cancellationToken);
        var products = await _session.Query<Product>()
            .Where(p => p.Company.Id == request.CompanyId && productIds.Contains(p.Id))
            .ToListAsync(cancellationToken);

        if (sections.Count != sectionIds.Count || products.Count != productIds.Count)
        {
            throw new ValidationException(ErrorMessages.InvalidTopLevelReorderItems);
        }

        var sectionsById = sections.ToDictionary(s => s.Id);
        var productsById = products.ToDictionary(p => p.Id);

        using var transaction = _session.BeginTransaction();

        for (var i = 0; i < request.OrderedItems.Count; i++)
        {
            var item = request.OrderedItems[i];
            if (item.IsSection)
            {
                var section = sectionsById[item.Id];
                section.SortOrder = i;
                await _session.UpdateAsync(section, cancellationToken);
            }
            else
            {
                var product = productsById[item.Id];
                product.SortOrder = i;
                await _session.UpdateAsync(product, cancellationToken);
            }
        }

        await transaction.CommitAsync(cancellationToken);
    }
}
