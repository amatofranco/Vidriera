using Microsoft.Extensions.Logging;

namespace Vidriera.Application.Catalogs;

internal sealed class CatalogJobProgressReporter
{
    private readonly Func<CatalogGenerationProgress, Task> _save;
    private readonly TimeSpan _minInterval;
    private readonly ILogger _logger;
    private readonly object _sync = new();
    private CatalogGenerationProgress? _latest;
    private bool _running;
    private Task _loop = Task.CompletedTask;

    public CatalogJobProgressReporter(Func<CatalogGenerationProgress, Task> save, TimeSpan minInterval, ILogger logger)
    {
        _save = save;
        _minInterval = minInterval;
        _logger = logger;
    }

    public void Report(CatalogGenerationProgress progress)
    {
        lock (_sync)
        {
            _latest = progress;
            if (!_running)
            {
                _running = true;
                _loop = Task.Run(SaveLoopAsync);
            }
        }
    }

    public Task FlushAsync()
    {
        lock (_sync)
        {
            return _loop;
        }
    }

    private async Task SaveLoopAsync()
    {
        while (true)
        {
            CatalogGenerationProgress progress;
            lock (_sync)
            {
                if (_latest is null)
                {
                    _running = false;
                    return;
                }

                progress = _latest;
                _latest = null;
            }

            var started = DateTime.UtcNow;
            try
            {
                await _save(progress);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo guardar el progreso de la generación del catálogo.");
            }

            var remaining = _minInterval - (DateTime.UtcNow - started);
            if (remaining > TimeSpan.Zero)
            {
                await Task.Delay(remaining);
            }
        }
    }
}
