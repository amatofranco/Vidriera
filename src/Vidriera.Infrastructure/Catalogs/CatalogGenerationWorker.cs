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
    private static readonly TimeSpan ExternalWorkerGraceWindow = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan OrphanedJobStaleThreshold = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan ErrorBackoff = TimeSpan.FromMinutes(1);

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
            TimeSpan? wait;
            try
            {
                wait = await TryProcessNextJobAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado procesando la cola de generación de catálogos.");
                wait = ErrorBackoff;
            }

            if (wait != TimeSpan.Zero)
            {
                try
                {
                    await _signal.WaitAsync(wait ?? Timeout.InfiniteTimeSpan, stoppingToken);
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

    private async Task<TimeSpan?> TryProcessNextJobAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var session = scope.ServiceProvider.GetRequiredService<ISession>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var oldestPending = await session.Query<CatalogGenerationJob>()
            .Where(j => j.Status == CatalogGenerationJobStatus.Pending)
            .OrderBy(j => j.CreatedAt)
            .FirstOrDefaultAsync();

        if (oldestPending is null)
        {
            return null;
        }

        if (!string.IsNullOrEmpty(_workerOptions.WorkerUrl))
        {
            var remaining = oldestPending.CreatedAt + ExternalWorkerGraceWindow - DateTime.UtcNow;
            if (remaining > TimeSpan.Zero)
            {
                return remaining;
            }
        }

        await mediator.Send(new ProcessCatalogGenerationJobCommand(oldestPending.Id), stoppingToken);
        return TimeSpan.Zero;
    }

    private static async Task SaveAsync(ISession session, CatalogGenerationJob job)
    {
        using var transaction = session.BeginTransaction();
        await session.UpdateAsync(job);
        await transaction.CommitAsync();
    }
}
