namespace Vidriera.Application.Catalogs;

/// <summary>Cuánto tarda cada etapa de una generación, para registrarlo en el log al terminar.</summary>
internal sealed class CatalogGenerationTimings
{
    public long TotalMs { get; set; }
    public long SizeCheckMs { get; set; }
    public long MergeAndRasterizePhaseMs { get; set; }
    public long MergeMs { get; set; }
    public long RasterMs { get; set; }
    public long UploadSlotWaitMs { get; set; }
    public long ProgressMs { get; set; }
    public long CpuYieldMs { get; set; }
    public long UploadDrainMs { get; set; }
    public long MergedPdfUploadMs { get; set; }
    public long DeletePreviousMs { get; set; }
}
