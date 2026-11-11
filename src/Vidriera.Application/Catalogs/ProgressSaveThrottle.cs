namespace Vidriera.Application.Catalogs;

internal sealed class ProgressSaveThrottle
{
    private readonly TimeSpan _minInterval;
    private string? _lastStage;
    private DateTime _lastSavedAt = DateTime.MinValue;

    public ProgressSaveThrottle(TimeSpan minInterval)
    {
        _minInterval = minInterval;
    }

    public bool ShouldSave(CatalogGenerationProgress progress, DateTime now)
    {
        var save = progress.Stage != _lastStage
            || progress.Current >= progress.Total
            || now - _lastSavedAt >= _minInterval;

        if (save)
        {
            _lastStage = progress.Stage;
            _lastSavedAt = now;
        }

        return save;
    }
}
