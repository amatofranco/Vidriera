namespace Vidriera.Application.Catalogs;

public class CatalogGenerationGate
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private int _waiting;

    public async Task<bool> TryEnterAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _waiting);
        try
        {
            return await _semaphore.WaitAsync(timeout, cancellationToken);
        }
        finally
        {
            Interlocked.Decrement(ref _waiting);
        }
    }

    public void Exit() => _semaphore.Release();

    public bool HasWaiters => Volatile.Read(ref _waiting) > 0;
}
