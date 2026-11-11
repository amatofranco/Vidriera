using Vidriera.Application.Abstractions;
using Vidriera.Application.Common;

namespace Vidriera.Application.Catalogs;

internal enum HdPagesOutcome
{
    Completed,
    YieldedToGeneration,
    CatalogReplaced
}

internal sealed record HdPagesResult(HdPagesOutcome Outcome, int GeneratedPages);

internal sealed class CatalogHdPagesGenerator
{
    private const int MaxConcurrentDeletes = 8;

    private readonly IBlobStorageService _pageStorage;
    private readonly IPdfRasterizerService _rasterizer;
    private readonly CatalogGenerationGate _gate;
    private readonly Func<CancellationToken, Task<bool>> _isStillCurrent;
    private readonly Func<CancellationToken, Task<bool>> _hasPendingJob;
    private readonly TimeSpan _checkInterval;

    public CatalogHdPagesGenerator(
        IBlobStorageService pageStorage,
        IPdfRasterizerService rasterizer,
        CatalogGenerationGate gate,
        Func<CancellationToken, Task<bool>> isStillCurrent,
        Func<CancellationToken, Task<bool>> hasPendingJob,
        TimeSpan checkInterval)
    {
        _pageStorage = pageStorage;
        _rasterizer = rasterizer;
        _gate = gate;
        _isStillCurrent = isStillCurrent;
        _hasPendingJob = hasPendingJob;
        _checkInterval = checkInterval;
    }

    public async Task<HdPagesResult> GenerateAsync(
        Guid companyId, Guid catalogId, byte[] pdfBytes, int pageCount, CancellationToken cancellationToken)
    {
        var generated = 0;
        var sinceLastCheck = System.Diagnostics.Stopwatch.StartNew();
        var pageIndices = Enumerable.Range(0, pageCount).ToList();

        await foreach (var (pageIndex, imageBytes, _, _) in _rasterizer.RasterizePagesToHdImagesAsync(pdfBytes, pageIndices, cancellationToken))
        {
            using (var imageStream = new MemoryStream(imageBytes))
            {
                await _pageStorage.UploadAsync(
                    BlobKeys.GeneratedCatalogPageHd(companyId, catalogId, pageIndex + 1), imageStream, "image/webp", cancellationToken);
            }
            generated++;

            if (_gate.HasWaiters)
            {
                return new HdPagesResult(HdPagesOutcome.YieldedToGeneration, generated);
            }

            if (sinceLastCheck.Elapsed < _checkInterval)
            {
                continue;
            }

            sinceLastCheck.Restart();
            if (!await _isStillCurrent(cancellationToken))
            {
                await DeleteGeneratedAsync(companyId, catalogId, generated);
                return new HdPagesResult(HdPagesOutcome.CatalogReplaced, generated);
            }

            if (await _hasPendingJob(cancellationToken))
            {
                return new HdPagesResult(HdPagesOutcome.YieldedToGeneration, generated);
            }
        }

        if (!await _isStillCurrent(cancellationToken))
        {
            await DeleteGeneratedAsync(companyId, catalogId, generated);
            return new HdPagesResult(HdPagesOutcome.CatalogReplaced, generated);
        }

        return new HdPagesResult(HdPagesOutcome.Completed, generated);
    }

    private Task DeleteGeneratedAsync(Guid companyId, Guid catalogId, int generated) =>
        Parallel.ForEachAsync(
            Enumerable.Range(1, generated),
            new ParallelOptions { MaxDegreeOfParallelism = MaxConcurrentDeletes },
            async (pageNumber, ct) => await _pageStorage.DeleteAsync(
                BlobKeys.GeneratedCatalogPageHd(companyId, catalogId, pageNumber), ct));
}
