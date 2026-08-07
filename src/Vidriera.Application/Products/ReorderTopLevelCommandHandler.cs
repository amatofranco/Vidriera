using MediatR;
using NHibernate;
using NHibernate.Linq;
using Vidriera.Application.Common;
using Vidriera.Domain.Entities;

namespace Vidriera.Application.Products;

public class ReorderTopLevelCommandHandler : IRequestHandler<ReorderTopLevelCommand>
{
    private readonly ISession _session;

    public ReorderTopLevelCommandHandler(ISession session)
    {
        _session = session;
    }

    public Task Handle(ReorderTopLevelCommand request, CancellationToken cancellationToken)
    {
        var sectionScope = _session.Query<Section>()
            .Where(s => s.Company.Id == request.CompanyId && s.ParentSection == null);
        var productScope = _session.Query<Product>()
            .Where(p => p.Company.Id == request.CompanyId && p.Section == null);

        return ReorderApplier.ApplyAsync(
            _session,
            request.OrderedItems,
            sectionScope,
            productScope,
            ErrorMessages.InvalidTopLevelReorderItems,
            cancellationToken);
    }
}
