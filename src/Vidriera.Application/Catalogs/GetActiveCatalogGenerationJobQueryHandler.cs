using MediatR;
using NHibernate;
using NHibernate.Linq;
using Vidriera.Domain.Entities;

namespace Vidriera.Application.Catalogs;

public class GetActiveCatalogGenerationJobQueryHandler : IRequestHandler<GetActiveCatalogGenerationJobQuery, CatalogGenerationJobStatusResult?>
{
    private readonly ISession _session;

    public GetActiveCatalogGenerationJobQueryHandler(ISession session)
    {
        _session = session;
    }

    public async Task<CatalogGenerationJobStatusResult?> Handle(GetActiveCatalogGenerationJobQuery request, CancellationToken cancellationToken)
    {
        var job = await _session.Query<CatalogGenerationJob>()
            .Where(j => j.Company.Id == request.CompanyId
                && (j.Status == CatalogGenerationJobStatus.Pending || j.Status == CatalogGenerationJobStatus.Running))
            .OrderByDescending(j => j.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (job is null)
        {
            return null;
        }

        return new CatalogGenerationJobStatusResult(job.Id, job.Status, job.Stage, job.ProgressCurrent, job.ProgressTotal, null, null);
    }
}
