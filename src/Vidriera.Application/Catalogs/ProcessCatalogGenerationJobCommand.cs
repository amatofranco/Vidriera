using MediatR;
using Microsoft.Extensions.Logging;
using NHibernate;
using Vidriera.Application.Common.Exceptions;
using Vidriera.Domain.Entities;

namespace Vidriera.Application.Catalogs;

public record ProcessCatalogGenerationJobCommand(Guid JobId) : IRequest<bool>;

public class ProcessCatalogGenerationJobCommandHandler : IRequestHandler<ProcessCatalogGenerationJobCommand, bool>
{
    private readonly ISession _session;
    private readonly IMediator _mediator;
    private readonly ILogger<ProcessCatalogGenerationJobCommandHandler> _logger;

    public ProcessCatalogGenerationJobCommandHandler(
        ISession session,
        IMediator mediator,
        ILogger<ProcessCatalogGenerationJobCommandHandler> logger)
    {
        _session = session;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<bool> Handle(ProcessCatalogGenerationJobCommand request, CancellationToken cancellationToken)
    {
        var job = await ClaimAsync(request.JobId, cancellationToken);
        if (job is null)
        {
            return false;
        }

        using var progressLock = new SemaphoreSlim(1, 1);

        try
        {
            var result = await _mediator.Send(
                new GenerateCatalogCommand(
                    job.Company.Id,
                    job.User.Id,
                    job.ShowPrices,
                    progress => ReportProgressAsync(progressLock, job, progress)),
                cancellationToken);

            job.Status = CatalogGenerationJobStatus.Succeeded;
            job.ResultCatalogId = result.Id;
            job.ResultUrl = result.Url;
            job.UpdatedAt = DateTime.UtcNow;
            await SaveAsync(job);
        }
        catch (Exception ex)
        {
            var isExpected = ex is ValidationException or NotFoundException;
            if (!isExpected)
            {
                _logger.LogError(ex, "Error inesperado generando el catálogo para el job {JobId}.", job.Id);
            }

            job.Status = CatalogGenerationJobStatus.Failed;
            job.ErrorMessage = isExpected
                ? ex.Message
                : "No se pudo generar el catálogo por un error inesperado del servidor. Volvé a intentar en unos minutos.";
            job.UpdatedAt = DateTime.UtcNow;
            await SaveAsync(job);
        }

        return true;
    }

    private async Task<CatalogGenerationJob?> ClaimAsync(Guid jobId, CancellationToken cancellationToken)
    {
        // LockMode.Upgrade (SELECT ... FOR UPDATE) serializa el claim entre el worker externo
        // y el local: si ambos intentan tomar el mismo job, el segundo espera a que el primero
        // confirme el cambio de estado y ve que ya no está Pending.
        using var transaction = _session.BeginTransaction();

        var job = await _session.GetAsync<CatalogGenerationJob>(jobId, LockMode.Upgrade, cancellationToken);
        if (job is null || job.Status != CatalogGenerationJobStatus.Pending)
        {
            await transaction.RollbackAsync(cancellationToken);
            return null;
        }

        job.Status = CatalogGenerationJobStatus.Running;
        job.UpdatedAt = DateTime.UtcNow;
        await _session.UpdateAsync(job, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return job;
    }

    private async Task ReportProgressAsync(SemaphoreSlim progressLock, CatalogGenerationJob job, CatalogGenerationProgress progress)
    {
        await progressLock.WaitAsync();
        try
        {
            job.Stage = progress.Stage;
            job.ProgressCurrent = progress.Current;
            job.ProgressTotal = progress.Total;
            job.UpdatedAt = DateTime.UtcNow;
            await SaveAsync(job);
        }
        finally
        {
            progressLock.Release();
        }
    }

    private async Task SaveAsync(CatalogGenerationJob job)
    {
        using var transaction = _session.BeginTransaction();
        await _session.UpdateAsync(job);
        await transaction.CommitAsync();
    }
}
