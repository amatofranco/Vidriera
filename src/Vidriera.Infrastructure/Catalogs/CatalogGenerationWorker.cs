using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NHibernate;
using NHibernate.Linq;
using Vidriera.Application.Catalogs;
using Vidriera.Domain.Entities;

namespace Vidriera.Infrastructure.Catalogs;

public class CatalogGenerationWorker : BackgroundService
{
    private static readonly TimeSpan FallbackPollInterval = TimeSpan.FromMinutes(5);

    // Cuando hay un worker externo configurado, se le da esta ventana para tomar el job
    // antes de que el worker local (más lento en CPU) lo procese como respaldo.
    private static readonly TimeSpan ExternalWorkerGraceWindow = TimeSpan.FromMinutes(2);

    // Un job "Running" puede estar siendo procesado por el worker externo (Cloud Run), cuyo
    // ciclo de vida es independiente de este proceso. Sólo se considera huérfano si no tuvo
    // actividad (progreso reportado) por más tiempo del que puede durar una generación real.
    private static readonly TimeSpan OrphanedJobStaleThreshold = TimeSpan.FromMinutes(30);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly CatalogGenerationSignal _signal;
    private readonly CatalogWorkerOptions _workerOptions;
    private readonly ILogger<CatalogGenerationWorker> _logger;

    public CatalogGenerationWorker(
        IServiceScopeFactory scopeFactory,
        CatalogGenerationSignal signal,
        IOptions<CatalogWorkerOptions> workerOptions,
        ILogger<CatalogGenerationWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _signal = signal;
        _workerOptions = workerOptions.Value;
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
                    await _signal.WaitAsync(FallbackPollInterval, stoppingToken);
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

        var cutoff = DateTime.UtcNow - OrphanedJobStaleThreshold;
        var orphaned = await session.Query<CatalogGenerationJob>()
            .Where(j => j.Status == CatalogGenerationJobStatus.Running && j.UpdatedAt <= cutoff)
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

        var jobId = await FindNextClaimableJobIdAsync(session);
        if (jobId is null)
        {
            return false;
        }

        return await mediator.Send(new ProcessCatalogGenerationJobCommand(jobId.Value), stoppingToken);
    }

    private async Task<Guid?> FindNextClaimableJobIdAsync(ISession session)
    {
        var query = session.Query<CatalogGenerationJob>()
            .Where(j => j.Status == CatalogGenerationJobStatus.Pending);

        if (!string.IsNullOrEmpty(_workerOptions.WorkerUrl))
        {
            var cutoff = DateTime.UtcNow - ExternalWorkerGraceWindow;
            query = query.Where(j => j.CreatedAt <= cutoff);
        }

        var job = await query.OrderBy(j => j.CreatedAt).FirstOrDefaultAsync();
        return job?.Id;
    }

    private static async Task SaveAsync(ISession session, CatalogGenerationJob job)
    {
        using var transaction = session.BeginTransaction();
        await session.UpdateAsync(job);
        await transaction.CommitAsync();
    }
}
