using MediatR;
using NHibernate;
using Vidriera.Application.Common;
using Vidriera.Application.Common.Exceptions;
using Vidriera.Domain.Entities;

namespace Vidriera.Application.Catalogs;

public class GetCatalogGenerationJobStatusQueryHandler : IRequestHandler<GetCatalogGenerationJobStatusQuery, CatalogGenerationJobStatusResult>
{
    private readonly ISession _session;

    public GetCatalogGenerationJobStatusQueryHandler(ISession session)
    {
        _session = session;
    }

    public async Task<CatalogGenerationJobStatusResult> Handle(GetCatalogGenerationJobStatusQuery request, CancellationToken cancellationToken)
    {
        var job = await _session.GetAsync<CatalogGenerationJob>(request.JobId, cancellationToken);
        if (job is null || job.Company.Id != request.CompanyId)
        {
            throw new NotFoundException(ErrorMessages.CatalogGenerationJobNotFound(request.JobId));
        }

        var result = job.Status == CatalogGenerationJobStatus.Succeeded && job.ResultCatalogId.HasValue
            ? new GenerateCatalogResult(job.ResultCatalogId.Value, job.ResultUrl ?? "")
            : null;

        return new CatalogGenerationJobStatusResult(
            job.Id,
            job.Status,
            job.Stage,
            job.ProgressCurrent,
            job.ProgressTotal,
            result,
            job.ErrorMessage);
    }
}
