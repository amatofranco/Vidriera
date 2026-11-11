using MediatR;
using Microsoft.Extensions.Logging;
using NHibernate;
using Vidriera.Application.Common.Exceptions;
using Vidriera.Domain.Entities;

namespace Vidriera.Application.Catalogs;

public record ProcessCatalogGenerationJobCommand(Guid JobId) : IRequest<bool>;

public class ProcessCatalogGenerationJobCommandHandler : IRequestHandler<ProcessCatalogGenerationJobCommand, bool>
{
    private static readonly TimeSpan ProgressSaveInterval = TimeSpan.FromMilliseconds(500);

    private readonly ISession _session;
    private readonly ISessionFactory _sessionFactory;
    private readonly IMediator _mediator;
    private readonly ILogger<ProcessCatalogGenerationJobCommandHandler> _logger;

    public ProcessCatalogGenerationJobCommandHandler(
        ISession session,
        ISessionFactory sessionFactory,
        IMediator mediator,
        ILogger<ProcessCatalogGenerationJobCommandHandler> logger)
    {
        _session = session;
        _sessionFactory = sessionFactory;
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

        var jobId = job.Id;
        var progressReporter = new CatalogJobProgressReporter(
            progress => SaveProgressAsync(jobId, progress), ProgressSaveInterval, _logger);
        CatalogGenerationProgress? lastProgress = null;

        try
        {
            var result = await _mediator.Send(
                new GenerateCatalogCommand(
                    job.Company.Id,
                    job.User.Id,
                    job.ShowPrices,
                    progress =>
                    {
                        lastProgress = progress;
                        progressReporter.Report(progress);
                        return Task.CompletedTask;
                    }),
                cancellationToken);

            await progressReporter.FlushAsync();
            ApplyProgress(job, lastProgress);
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

            await progressReporter.FlushAsync();
            ApplyProgress(job, lastProgress);
            job.Status = CatalogGenerationJobStatus.Failed;
            job.ErrorMessage = isExpected
                ? ex.Message
                : "No se pudo generar el catálogo por un error inesperado del servidor. Volvé a intentar en unos minutos.";
            job.UpdatedAt = DateTime.UtcNow;
            await SaveAsync(job);
        }

        if (job.Status == CatalogGenerationJobStatus.Succeeded && job.ResultCatalogId is { } catalogId)
        {
            await GenerateHdPagesAsync(catalogId, cancellationToken);
        }

        return true;
    }

    private async Task GenerateHdPagesAsync(Guid catalogId, CancellationToken cancellationToken)
    {
        try
        {
            await _mediator.Send(new GenerateCatalogHdPagesCommand(catalogId), cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo generar la versión en alta del catálogo {CatalogId}.", catalogId);
        }
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

    private async Task SaveProgressAsync(Guid jobId, CatalogGenerationProgress progress)
    {
        using var session = _sessionFactory.OpenSession();
        using var transaction = session.BeginTransaction();
        await session.CreateSQLQuery(
                "update catalog_generation_jobs set stage = :stage, progress_current = :current, " +
                "progress_total = :total, updated_at = :updatedAt where id = :id")
            .SetParameter("stage", progress.Stage)
            .SetParameter("current", progress.Current)
            .SetParameter("total", progress.Total)
            .SetParameter("updatedAt", DateTime.UtcNow)
            .SetParameter("id", jobId)
            .ExecuteUpdateAsync();
        await transaction.CommitAsync();
    }

    private static void ApplyProgress(CatalogGenerationJob job, CatalogGenerationProgress? progress)
    {
        if (progress is null)
        {
            return;
        }

        job.Stage = progress.Stage;
        job.ProgressCurrent = progress.Current;
        job.ProgressTotal = progress.Total;
    }

    private async Task SaveAsync(CatalogGenerationJob job)
    {
        using var transaction = _session.BeginTransaction();
        await _session.UpdateAsync(job);
        await transaction.CommitAsync();
    }
}
