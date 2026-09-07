using MediatR;
using NHibernate;
using NHibernate.Linq;
using Vidriera.Application.Common;
using Vidriera.Application.Common.Exceptions;
using Vidriera.Domain.Entities;

namespace Vidriera.Application.Catalogs;

public class EnqueueCatalogGenerationCommandHandler : IRequestHandler<EnqueueCatalogGenerationCommand, EnqueueCatalogGenerationResult>
{
    private readonly ISession _session;

    public EnqueueCatalogGenerationCommandHandler(ISession session)
    {
        _session = session;
    }

    public async Task<EnqueueCatalogGenerationResult> Handle(EnqueueCatalogGenerationCommand request, CancellationToken cancellationToken)
    {
        var items = await _session.Query<Item>()
            .Where(p => p.Company.Id == request.CompanyId)
            .ToListAsync(cancellationToken);

        var selectedIds = new HashSet<Guid>(
            items.Where(p => p.HasStock && !string.IsNullOrEmpty(p.SheetPdfBlobKey)).Select(p => p.Id));

        if (selectedIds.Count == 0)
        {
            throw new ValidationException(ErrorMessages.MustSelectAtLeastOneItem);
        }

        if (request.ShowPrices)
        {
            var missingCount = items.Count(p => selectedIds.Contains(p.Id) && p.Price is null);
            if (missingCount > 0)
            {
                throw new ValidationException(ErrorMessages.MissingPricesForCatalog(missingCount));
            }
        }

        var activeJob = await _session.Query<CatalogGenerationJob>()
            .Where(j => j.Company.Id == request.CompanyId
                && (j.Status == CatalogGenerationJobStatus.Pending || j.Status == CatalogGenerationJobStatus.Running))
            .OrderByDescending(j => j.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (activeJob is not null)
        {
            return new EnqueueCatalogGenerationResult(activeJob.Id);
        }

        var company = await _session.GetAsync<Company>(request.CompanyId, cancellationToken);
        var user = await _session.GetAsync<User>(request.UserId, cancellationToken);
        var now = DateTime.UtcNow;

        var job = new CatalogGenerationJob
        {
            Company = company,
            User = user,
            ShowPrices = request.ShowPrices,
            Status = CatalogGenerationJobStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _session.SaveInTransactionAsync(job, cancellationToken);

        return new EnqueueCatalogGenerationResult(job.Id);
    }
}
