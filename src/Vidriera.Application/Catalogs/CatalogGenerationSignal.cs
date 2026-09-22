namespace Vidriera.Application.Catalogs;

public class CatalogGenerationSignal
{
    private readonly SemaphoreSlim _semaphore = new(0, int.MaxValue);

    public void Notify() => _semaphore.Release();

    public Task WaitAsync(TimeSpan timeout, CancellationToken cancellationToken) =>
        _semaphore.WaitAsync(timeout, cancellationToken);
}
