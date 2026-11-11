namespace Vidriera.Application.Catalogs;

/// <summary>
/// Decide cuándo vale la pena guardar el progreso de una generación en la base. Antes se guardaba
/// en cada página (una transacción por página, en el camino crítico); la barra de progreso se ve
/// igual guardando como mucho una vez por intervalo, más los cambios de etapa y el final de cada una.
/// </summary>
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
