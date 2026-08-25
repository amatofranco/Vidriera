using MediatR;
using NHibernate;
using NHibernate.Linq;
using Vidriera.Application.Common;
using Vidriera.Domain.Entities;

namespace Vidriera.Application.Items;

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
        var itemScope = _session.Query<Item>()
            .Where(p => p.Company.Id == request.CompanyId && p.Section == null);

        return ReorderApplier.ApplyAsync(
            _session,
            request.OrderedItems,
            sectionScope,
            itemScope,
            ErrorMessages.InvalidTopLevelReorderItems,
            cancellationToken);
    }
}
