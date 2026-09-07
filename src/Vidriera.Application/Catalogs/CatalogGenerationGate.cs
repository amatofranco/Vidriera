namespace Vidriera.Application.Catalogs;

public class CatalogGenerationGate
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task<bool> TryEnterAsync(TimeSpan timeout, CancellationToken cancellationToken) =>
        await _semaphore.WaitAsync(timeout, cancellationToken);

    public void Exit() => _semaphore.Release();
}
