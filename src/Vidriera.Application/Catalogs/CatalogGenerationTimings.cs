namespace Vidriera.Application.Catalogs;

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
