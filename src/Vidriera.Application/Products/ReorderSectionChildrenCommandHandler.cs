using MediatR;
using NHibernate;
using NHibernate.Linq;
using Vidriera.Application.Common;
using Vidriera.Domain.Entities;

namespace Vidriera.Application.Products;

public class ReorderSectionChildrenCommandHandler : IRequestHandler<ReorderSectionChildrenCommand>
{
    private readonly ISession _session;

    public ReorderSectionChildrenCommandHandler(ISession session)
    {
        _session = session;
    }

    public Task Handle(ReorderSectionChildrenCommand request, CancellationToken cancellationToken)
    {
        var sectionScope = _session.Query<Section>()
            .Where(s => s.Company.Id == request.CompanyId && s.ParentSection != null && s.ParentSection.Id == request.ParentSectionId);
        var productScope = _session.Query<Product>()
            .Where(p => p.Company.Id == request.CompanyId && p.Section != null && p.Section.Id == request.ParentSectionId);

        return ReorderApplier.ApplyAsync(
            _session,
            request.OrderedItems,
            sectionScope,
            productScope,
            ErrorMessages.InvalidSectionReorderItems,
            cancellationToken);
    }
}
