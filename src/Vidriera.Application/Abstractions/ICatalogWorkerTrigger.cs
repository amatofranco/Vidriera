namespace Vidriera.Application.Abstractions;

public interface ICatalogWorkerTrigger
{
    Task TriggerAsync(Guid jobId, CancellationToken cancellationToken);
}
