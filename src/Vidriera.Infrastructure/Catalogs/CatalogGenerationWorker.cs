using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NHibernate;
using NHibernate.Linq;
using Vidriera.Application.Catalogs;
using Vidriera.Domain.Entities;

namespace Vidriera.Infrastructure.Catalogs;

public class CatalogGenerationWorker : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CatalogGenerationWorker> _logger;

    public CatalogGenerationWorker(IServiceScopeFactory scopeFactory, ILogger<CatalogGenerationWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RecoverOrphanedJobsAsync();

        while (!stoppingToken.IsCancellationRequested)
        {
            var processed = false;
            try
            {
                processed = await TryProcessNextJobAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado procesando la cola de generación de catálogos.");
            }

            if (!processed)
            {
                try
                {
                    await Task.Delay(PollInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }

    private async Task RecoverOrphanedJobsAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var session = scope.ServiceProvider.GetRequiredService<ISession>();

        var orphaned = await session.Query<CatalogGenerationJob>()
            .Where(j => j.Status == CatalogGenerationJobStatus.Running)
            .ToListAsync();

        foreach (var job in orphaned)
        {
            job.Status = CatalogGenerationJobStatus.Failed;
            job.ErrorMessage = "El servidor se reinició durante la generación. Volvé a intentar.";
            job.UpdatedAt = DateTime.UtcNow;
            await SaveAsync(session, job);
        }
    }

    private async Task<bool> TryProcessNextJobAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var session = scope.ServiceProvider.GetRequiredService<ISession>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var job = await ClaimNextPendingJobAsync(session);
        if (job is null)
        {
            return false;
        }

        using var progressLock = new SemaphoreSlim(1, 1);

        try
        {
            var result = await mediator.Send(
                new GenerateCatalogCommand(
                    job.Company.Id,
                    job.User.Id,
                    job.ShowPrices,
                    progress => ReportProgressAsync(session, progressLock, job, progress)),
                stoppingToken);

            job.Status = CatalogGenerationJobStatus.Succeeded;
            job.ResultCatalogId = result.Id;
            job.ResultUrl = result.Url;
            job.UpdatedAt = DateTime.UtcNow;
            await SaveAsync(session, job);
        }
        catch (Exception ex)
        {
            job.Status = CatalogGenerationJobStatus.Failed;
            job.ErrorMessage = ex.Message;
            job.UpdatedAt = DateTime.UtcNow;
            await SaveAsync(session, job);
        }

        return true;
    }

    private static async Task<CatalogGenerationJob?> ClaimNextPendingJobAsync(ISession session)
    {
        var job = await session.Query<CatalogGenerationJob>()
            .Where(j => j.Status == CatalogGenerationJobStatus.Pending)
            .OrderBy(j => j.CreatedAt)
            .FirstOrDefaultAsync();

        if (job is null)
        {
            return null;
        }

        job.Status = CatalogGenerationJobStatus.Running;
        job.UpdatedAt = DateTime.UtcNow;
        await SaveAsync(session, job);

        return job;
    }

    private static async Task ReportProgressAsync(
        ISession session,
        SemaphoreSlim progressLock,
        CatalogGenerationJob job,
        CatalogGenerationProgress progress)
    {
        await progressLock.WaitAsync();
        try
        {
            job.Stage = progress.Stage;
            job.ProgressCurrent = progress.Current;
            job.ProgressTotal = progress.Total;
            job.UpdatedAt = DateTime.UtcNow;
            await SaveAsync(session, job);
        }
        finally
        {
            progressLock.Release();
        }
    }

    private static async Task SaveAsync(ISession session, CatalogGenerationJob job)
    {
        using var transaction = session.BeginTransaction();
        await session.UpdateAsync(job);
        await transaction.CommitAsync();
    }
}
