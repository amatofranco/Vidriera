using MediatR;
using NHibernate;
using NHibernate.Linq;
using Vidriera.Application.Common;
using Vidriera.Application.Common.Exceptions;
using Vidriera.Domain.Entities;

namespace Vidriera.Application.Products;

public class AssignProductSectionCommandHandler : IRequestHandler<AssignProductSectionCommand>
{
    private readonly ISession _session;

    public AssignProductSectionCommandHandler(ISession session)
    {
        _session = session;
    }

    public async Task Handle(AssignProductSectionCommand request, CancellationToken cancellationToken)
    {
        var product = await _session.Query<Product>()
            .FirstOrDefaultAsync(p => p.Id == request.ProductId && p.Company.Id == request.CompanyId, cancellationToken);

        if (product is null)
        {
            throw new NotFoundException($"No se encontró el producto {request.ProductId} para esta empresa.");
        }

        Section? section = null;
        if (request.SectionId.HasValue)
        {
            section = await _session.Query<Section>()
                .FirstOrDefaultAsync(s => s.Id == request.SectionId.Value && s.Company.Id == request.CompanyId, cancellationToken);

            if (section is null)
            {
                throw new NotFoundException($"No se encontró la carátula {request.SectionId.Value} para esta empresa.");
            }
        }

        // Appended at the end of whichever container it's moving into -- the same "drop it
        // at the bottom, drag it into place afterwards" behavior a newly created item gets.
        var nextSortOrder = section is null
            ? await TopLevelOrdering.NextTopLevelSortOrderAsync(_session, request.CompanyId, cancellationToken)
            : await TopLevelOrdering.NextSectionSortOrderAsync(_session, section.Id, cancellationToken);

        product.Section = section;
        product.SortOrder = nextSortOrder;

        using var transaction = _session.BeginTransaction();
        await _session.UpdateAsync(product, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
