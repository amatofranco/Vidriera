using System.Diagnostics;
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NHibernate;
using NHibernate.Linq;
using Vidriera.Application.Abstractions;
using Vidriera.Application.Common;
using Vidriera.Application.Common.Exceptions;
using Vidriera.Domain.Entities;

namespace Vidriera.Application.Catalogs;

public class GenerateCatalogCommandHandler : IRequestHandler<GenerateCatalogCommand, GenerateCatalogResult>
{
    private static readonly TimeSpan GateTimeout = TimeSpan.FromSeconds(10);

    private readonly ISession _session;
    private readonly IBlobStorageService _blobStorageService;
    private readonly IPublicCatalogPageStorageService _publicCatalogPageStorageService;
    private readonly IPdfMergeService _pdfMergeService;
    private readonly IPdfRasterizerService _pdfRasterizerService;
    private readonly CatalogGenerationGate _generationGate;
    private readonly CatalogOptions _options;
    private readonly ILogger<GenerateCatalogCommandHandler> _logger;

    public GenerateCatalogCommandHandler(
        ISession session,
        IBlobStorageService blobStorageService,
        IPublicCatalogPageStorageService publicCatalogPageStorageService,
        IPdfMergeService pdfMergeService,
        IPdfRasterizerService pdfRasterizerService,
        CatalogGenerationGate generationGate,
        IOptions<CatalogOptions> options,
        ILogger<GenerateCatalogCommandHandler> logger)
    {
        _session = session;
        _blobStorageService = blobStorageService;
        _publicCatalogPageStorageService = publicCatalogPageStorageService;
        _pdfMergeService = pdfMergeService;
        _pdfRasterizerService = pdfRasterizerService;
        _generationGate = generationGate;
        _options = options.Value;
        _logger = logger;
    }

    private bool PagesArePublic => !string.IsNullOrEmpty(_options.PagesPublicBaseUrl);

    private IBlobStorageService PageStorageService => PagesArePublic ? _publicCatalogPageStorageService : _blobStorageService;

    public async Task<GenerateCatalogResult> Handle(GenerateCatalogCommand request, CancellationToken cancellationToken)
    {
        if (!await _generationGate.TryEnterAsync(GateTimeout, cancellationToken))
        {
            throw new ValidationException(ErrorMessages.CatalogGenerationBusy);
        }

        try
        {
            return await HandleInternalAsync(request, cancellationToken);
        }
        finally
        {
            _generationGate.Exit();
        }
    }

    private async Task<GenerateCatalogResult> HandleInternalAsync(GenerateCatalogCommand request, CancellationToken cancellationToken)
    {
        var (allItems, allSections) = await LoadCatalogDataAsync(request.CompanyId, cancellationToken);
        var selectedIds = new HashSet<Guid>(
            allItems.Where(p => p.HasStock && !string.IsNullOrEmpty(p.SheetPdfBlobKey)).Select(p => p.Id));

        if (selectedIds.Count == 0)
        {
            throw new ValidationException(ErrorMessages.MustSelectAtLeastOneItem);
        }

        if (request.ShowPrices)
        {
            var missingCount = allItems.Count(p => selectedIds.Contains(p.Id) && p.Price is null);
            if (missingCount > 0)
            {
                throw new ValidationException(ErrorMessages.MissingPricesForCatalog(missingCount));
            }
        }

        var entries = CatalogMergePlanBuilder.BuildEntries(allSections, allItems, selectedIds);
        var fingerprint = CatalogMergePlanBuilder.ComputeContentFingerprint(entries);

        var company = await _session.GetAsync<Company>(request.CompanyId, cancellationToken);
        var existingCatalog = company.CurrentCatalogId.HasValue
            ? await _session.GetAsync<GeneratedCatalog>(company.CurrentCatalogId.Value, cancellationToken)
            : null;

        if (existingCatalog is not null && CatalogRegenerationDecision.ShouldReuseCachedCatalog(
                existingCatalog.ContentFingerprint, fingerprint, existingCatalog.PagesPublic, PagesArePublic))
        {
            return await RefreshSnapshotOnlyAsync(request, existingCatalog, entries, cancellationToken);
        }

        var estimatedPages = EstimateTotalPages(entries);
        if (estimatedPages > _options.MaxTotalPages)
        {
            throw new ValidationException(ErrorMessages.CatalogTooLarge(estimatedPages, _options.MaxTotalPages));
        }

        var timings = new CatalogGenerationTimings();
        var totalStopwatch = Stopwatch.StartNew();

        var physicalEntries = GetPhysicalEntries(entries);
        var phaseStopwatch = Stopwatch.StartNew();
        var estimatedBytes = await EstimateTotalBytesAsync(physicalEntries, cancellationToken);
        timings.SizeCheckMs = phaseStopwatch.ElapsedMilliseconds;
        if (estimatedBytes > _options.MaxTotalBytes)
        {
            throw new ValidationException(ErrorMessages.CatalogTooHeavy(estimatedBytes, _options.MaxTotalBytes));
        }

        var catalogId = Guid.NewGuid();
        phaseStopwatch.Restart();
        var mergePlan = await BuildMergeAndRasterizeAsync(
            physicalEntries, request.CompanyId, catalogId, estimatedPages, request.OnProgress, timings, cancellationToken);
        timings.MergeAndRasterizePhaseMs = phaseStopwatch.ElapsedMilliseconds;
        var mergeResult = mergePlan.MergeResult;
        var indexSnapshot = CatalogMergePlanBuilder.BuildIndexSnapshot(entries, mergeResult.PageCounts, request.ShowPrices);

        await ReportFinalizingAsync(request.OnProgress, 1);
        phaseStopwatch.Restart();
        var generatedBlobKey = await UploadMergedPdfAsync(request.CompanyId, mergeResult.Bytes, cancellationToken);
        timings.MergedPdfUploadMs = phaseStopwatch.ElapsedMilliseconds;
        var previousCatalogId = company.CurrentCatalogId;

        await ReportFinalizingAsync(request.OnProgress, 2);
        var catalog = await CreateCatalogAsync(
            catalogId, request, company, mergePlan.IncludedItems, indexSnapshot, generatedBlobKey, fingerprint,
            mergeResult.PageCounts.Sum(), mergePlan.FirstPageAspectRatio, cancellationToken);

        company.CurrentCatalogId = catalog.Id;
        await _session.UpdateInTransactionAsync(company, cancellationToken);

        if (previousCatalogId.HasValue)
        {
            await ReportFinalizingAsync(request.OnProgress, 3);
            phaseStopwatch.Restart();
            await DeleteCatalogAsync(request.CompanyId, previousCatalogId.Value, CancellationToken.None);
            timings.DeletePreviousMs = phaseStopwatch.ElapsedMilliseconds;
        }

        timings.TotalMs = totalStopwatch.ElapsedMilliseconds;
        LogTimings(catalog.Id, mergeResult, physicalEntries.Count, timings);

        var url = CatalogShareUrl.Build(_options.PublicBaseUrl, request.CompanyId, company.Slug);
        return new GenerateCatalogResult(catalog.Id, url);
    }

    private void LogTimings(Guid catalogId, PdfMergeResult mergeResult, int entryCount, CatalogGenerationTimings t) =>
        _logger.LogInformation(
            "Catálogo {CatalogId} generado: {Pages} páginas, {Entries} PDFs, {MergedMb:F1} MB. Tiempos (ms): total {TotalMs} | " +
            "chequeo de tamaños {SizeCheckMs} | unir+rasterizar {PhaseMs} (unir {MergeMs}, rasterizar {RasterMs}, " +
            "espera de lugar para subir {UploadWaitMs}, progreso {ProgressMs}, pausas de CPU {YieldMs}, " +
            "subidas pendientes al final {UploadDrainMs}) | subida del PDF {MergedPdfUploadMs} | borrar anterior {DeletePreviousMs}",
            catalogId, mergeResult.PageCounts.Sum(), entryCount, mergeResult.Bytes.Length / 1024d / 1024d, t.TotalMs,
            t.SizeCheckMs, t.MergeAndRasterizePhaseMs, t.MergeMs, t.RasterMs,
            t.UploadSlotWaitMs, t.ProgressMs, t.CpuYieldMs, t.UploadDrainMs, t.MergedPdfUploadMs, t.DeletePreviousMs);

    private const int FinalizingSteps = 3;

    private static Task ReportFinalizingAsync(Func<CatalogGenerationProgress, Task>? onProgress, int step) =>
        onProgress is null
            ? Task.CompletedTask
            : onProgress(new CatalogGenerationProgress("finalizing", step, FinalizingSteps));

    private async Task<(List<Item> Items, List<Section> Sections)> LoadCatalogDataAsync(
        Guid companyId,
        CancellationToken cancellationToken)
    {
        var items = await _session.Query<Item>()
            .Where(p => p.Company.Id == companyId)
            .ToListAsync(cancellationToken);
        var sections = await _session.Query<Section>()
            .Where(s => s.Company.Id == companyId)
            .ToListAsync(cancellationToken);

        return (items, sections);
    }

    private int EstimateTotalPages(IReadOnlyList<MergeEntry> entries)
    {
        var total = 0;
        foreach (var entry in entries)
        {
            total += entry switch
            {
                ItemEntry itemEntry => itemEntry.Item.PageCount,
                SectionCoverEntry cover when cover.Section.CoverPdfBlobKey is not null => cover.Section.CoverPageCount,
                _ => 0
            };
        }
        return total;
    }

    private static List<MergeEntry> GetPhysicalEntries(IReadOnlyList<MergeEntry> entries) =>
        entries
            .Where(entry => entry is ItemEntry || (entry is SectionCoverEntry cover && cover.Section.CoverPdfBlobKey is not null))
            .ToList();

    private static string GetBlobKey(MergeEntry entry) => entry switch
    {
        SectionCoverEntry cover => cover.Section.CoverPdfBlobKey!,
        ItemEntry itemEntry => itemEntry.Item.SheetPdfBlobKey!,
        _ => throw new InvalidOperationException("Unknown merge entry type.")
    };

    private const int MaxConcurrentSizeChecks = 8;

    private async Task<long> EstimateTotalBytesAsync(IReadOnlyList<MergeEntry> physicalEntries, CancellationToken cancellationToken)
    {
        using var sizeCheckSemaphore = new SemaphoreSlim(MaxConcurrentSizeChecks);
        var sizeTasks = physicalEntries.Select(async entry =>
        {
            await sizeCheckSemaphore.WaitAsync(cancellationToken);
            try
            {
                return await _blobStorageService.GetSizeAsync(GetBlobKey(entry), cancellationToken);
            }
            finally
            {
                sizeCheckSemaphore.Release();
            }
        });

        var sizes = await Task.WhenAll(sizeTasks);
        return sizes.Sum();
    }

    private sealed record MergePlan(PdfMergeResult MergeResult, List<Item> IncludedItems, double? FirstPageAspectRatio);

    private const int MaxConcurrentDownloads = 4;

    private async Task<MergePlan> BuildMergeAndRasterizeAsync(
        IReadOnlyList<MergeEntry> physicalEntries,
        Guid companyId,
        Guid catalogId,
        int estimatedTotalPages,
        Func<CatalogGenerationProgress, Task>? onProgress,
        CatalogGenerationTimings timings,
        CancellationToken cancellationToken)
    {
        var includedItems = new List<Item>();
        foreach (var entry in physicalEntries)
        {
            if (entry is ItemEntry itemEntry)
            {
                includedItems.Add(itemEntry.Item);
            }
        }

        var reportPages = physicalEntries.All(entry => GetKnownPageCount(entry) > 0);

        if (onProgress is not null)
        {
            await onProgress(new CatalogGenerationProgress(
                "rasterizing", 0, reportPages ? estimatedTotalPages : physicalEntries.Count));
        }

        using var session = _pdfMergeService.CreateSession();
        var pageStorageService = PageStorageService;
        var coordinator = new OrderedMergeRasterizeCoordinator(
            session, _pdfRasterizerService, pageStorageService, companyId, catalogId,
            physicalEntries.Count, estimatedTotalPages, reportPages, onProgress, _options.IsWorkerNode, timings);

        var pendingDownloads = new List<Task>(physicalEntries.Count);
        using var downloadSemaphore = new SemaphoreSlim(MaxConcurrentDownloads);

        try
        {
            for (var index = 0; index < physicalEntries.Count; index++)
            {
                var blobKey = GetBlobKey(physicalEntries[index]);

                await downloadSemaphore.WaitAsync(cancellationToken);
                pendingDownloads.Add(DownloadMergeAndRasterizeEntryAsync(
                    blobKey, index, coordinator, downloadSemaphore, cancellationToken));
                pendingDownloads.RemoveAll(t => t.Status == TaskStatus.RanToCompletion);
            }

            await Task.WhenAll(pendingDownloads);
            await coordinator.WaitForUploadsAsync();
        }
        catch
        {
            await WhenAllQuietlyAsync(pendingDownloads);
            await coordinator.WaitForUploadsQuietlyAsync();
            foreach (var pageKey in coordinator.UploadedPageKeys)
            {
                await pageStorageService.DeleteAsync(pageKey, CancellationToken.None);
            }
            throw;
        }

        await SyncStoredPageCountsAsync(physicalEntries, coordinator.ResolvedPageCounts, cancellationToken);

        return new MergePlan(session.Complete(), includedItems, coordinator.FirstPageAspectRatio);
    }

    private static async Task WhenAllQuietlyAsync(IEnumerable<Task> tasks)
    {
        try
        {
            await Task.WhenAll(tasks);
        }
        catch
        {
        }
    }

    private static int GetKnownPageCount(MergeEntry entry) => entry switch
    {
        ItemEntry itemEntry => itemEntry.Item.PageCount,
        SectionCoverEntry cover => cover.Section.CoverPageCount,
        _ => 0
    };

    private async Task SyncStoredPageCountsAsync(
        IReadOnlyList<MergeEntry> physicalEntries,
        IReadOnlyDictionary<int, int> resolvedPageCounts,
        CancellationToken cancellationToken)
    {
        foreach (var (index, pageCount) in resolvedPageCounts)
        {
            switch (physicalEntries[index])
            {
                case ItemEntry itemEntry when itemEntry.Item.PageCount != pageCount:
                    itemEntry.Item.PageCount = pageCount;
                    await _session.UpdateInTransactionAsync(itemEntry.Item, cancellationToken);
                    break;
                case SectionCoverEntry cover when cover.Section.CoverPageCount != pageCount:
                    cover.Section.CoverPageCount = pageCount;
                    await _session.UpdateInTransactionAsync(cover.Section, cancellationToken);
                    break;
            }
        }
    }

    private async Task DownloadMergeAndRasterizeEntryAsync(
        string blobKey,
        int index,
        OrderedMergeRasterizeCoordinator coordinator,
        SemaphoreSlim downloadSemaphore,
        CancellationToken cancellationToken)
    {
        try
        {
            var bytes = await DownloadBytesAsync(blobKey, cancellationToken);
            await coordinator.AddDownloadedAsync(index, bytes, cancellationToken);
        }
        finally
        {
            downloadSemaphore.Release();
        }
    }

    private sealed class OrderedMergeRasterizeCoordinator
    {
        private const int PagesPerCpuYield = 10;
        private static readonly TimeSpan CpuYieldDelay = TimeSpan.FromMilliseconds(300);

        private const int MaxConcurrentUploads = 6;

        private readonly IPdfMergeSession _mergeSession;
        private readonly IPdfRasterizerService _rasterizerService;
        private readonly IBlobStorageService _blobStorageService;
        private readonly Guid _companyId;
        private readonly Guid _catalogId;
        private readonly int _totalEntries;
        private readonly int _estimatedTotalPages;
        private readonly bool _reportPages;
        private readonly Func<CatalogGenerationProgress, Task>? _onProgress;
        private readonly bool _skipCpuYield;
        private readonly CatalogGenerationTimings _timings;
        private readonly SemaphoreSlim _uploadSlots = new(MaxConcurrentUploads, MaxConcurrentUploads);
        private readonly List<Task> _uploads = new();
        private readonly List<string> _uploadedPageKeys = new();
        private readonly Dictionary<int, byte[]> _pending = new();
        private readonly Dictionary<int, int> _resolvedPageCounts = new();
        private readonly SemaphoreSlim _lock = new(1, 1);
        private int _nextIndex;
        private int _pageCursor;
        private int _rasterizedPageCount;
        private double? _firstPageAspectRatio;

        public OrderedMergeRasterizeCoordinator(
            IPdfMergeSession mergeSession,
            IPdfRasterizerService rasterizerService,
            IBlobStorageService blobStorageService,
            Guid companyId,
            Guid catalogId,
            int totalEntries,
            int estimatedTotalPages,
            bool reportPages,
            Func<CatalogGenerationProgress, Task>? onProgress,
            bool skipCpuYield,
            CatalogGenerationTimings timings)
        {
            _mergeSession = mergeSession;
            _rasterizerService = rasterizerService;
            _blobStorageService = blobStorageService;
            _companyId = companyId;
            _catalogId = catalogId;
            _totalEntries = totalEntries;
            _estimatedTotalPages = estimatedTotalPages;
            _reportPages = reportPages;
            _onProgress = onProgress;
            _skipCpuYield = skipCpuYield;
            _timings = timings;
        }

        public IReadOnlyList<string> UploadedPageKeys => _uploadedPageKeys;

        public IReadOnlyDictionary<int, int> ResolvedPageCounts => _resolvedPageCounts;

        public double? FirstPageAspectRatio => _firstPageAspectRatio;

        public async Task AddDownloadedAsync(int index, byte[] pdfBytes, CancellationToken cancellationToken)
        {
            await _lock.WaitAsync(cancellationToken);
            try
            {
                _pending[index] = pdfBytes;

                while (_pending.Remove(_nextIndex, out var nextBytes))
                {
                    var mergeStart = Stopwatch.GetTimestamp();
                    var pageCount = _mergeSession.AddDocument(nextBytes);
                    _timings.MergeMs += ElapsedMs(mergeStart);
                    _resolvedPageCounts[_nextIndex] = pageCount;

                    var rasterStart = Stopwatch.GetTimestamp();
                    await foreach (var (pageIndex, imageBytes, width, height) in _rasterizerService.RasterizePagesToImagesAsync(
                        nextBytes, Enumerable.Range(0, pageCount).ToList(), cancellationToken))
                    {
                        _timings.RasterMs += ElapsedMs(rasterStart);
                        ThrowIfAnyUploadFailed();

                        var pageNumber = _pageCursor + pageIndex + 1;
                        if (pageNumber == 1)
                        {
                            _firstPageAspectRatio = (double)width / height;
                        }

                        var blobKey = BlobKeys.GeneratedCatalogPage(_companyId, _catalogId, pageNumber);
                        var slotWaitStart = Stopwatch.GetTimestamp();
                        await _uploadSlots.WaitAsync(cancellationToken);
                        _timings.UploadSlotWaitMs += ElapsedMs(slotWaitStart);
                        _uploadedPageKeys.Add(blobKey);
                        _uploads.Add(UploadPageAsync(blobKey, imageBytes, cancellationToken));

                        _rasterizedPageCount++;
                        if (_reportPages)
                        {
                            var progressStart = Stopwatch.GetTimestamp();
                            await ReportAsync("rasterizing", _rasterizedPageCount, _estimatedTotalPages);
                            _timings.ProgressMs += ElapsedMs(progressStart);
                        }

                        if (!_skipCpuYield && _rasterizedPageCount % PagesPerCpuYield == 0)
                        {
                            var yieldStart = Stopwatch.GetTimestamp();
                            await Task.Delay(CpuYieldDelay, cancellationToken);
                            _timings.CpuYieldMs += ElapsedMs(yieldStart);
                        }

                        rasterStart = Stopwatch.GetTimestamp();
                    }

                    _pageCursor += pageCount;
                    _nextIndex++;

                    if (!_reportPages)
                    {
                        await ReportAsync("rasterizing", _nextIndex, _totalEntries);
                    }
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        private async Task UploadPageAsync(string blobKey, byte[] imageBytes, CancellationToken cancellationToken)
        {
            try
            {
                using var stream = new MemoryStream(imageBytes);
                await _blobStorageService.UploadAsync(blobKey, stream, "image/webp", cancellationToken);
            }
            finally
            {
                _uploadSlots.Release();
            }
        }

        private void ThrowIfAnyUploadFailed()
        {
            var failed = _uploads.FirstOrDefault(t => t.IsFaulted || t.IsCanceled);
            failed?.GetAwaiter().GetResult();
        }

        public async Task WaitForUploadsAsync()
        {
            var drainStart = Stopwatch.GetTimestamp();
            await Task.WhenAll(_uploads);
            _timings.UploadDrainMs = ElapsedMs(drainStart);
        }

        public Task WaitForUploadsQuietlyAsync() => WhenAllQuietlyAsync(_uploads);

        private static long ElapsedMs(long startTimestamp) =>
            (long)Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;

        private async Task ReportAsync(string stage, int current, int total)
        {
            if (_onProgress is null)
            {
                return;
            }

            await _onProgress(new CatalogGenerationProgress(stage, current, total));
        }
    }

    private async Task<string> UploadMergedPdfAsync(Guid companyId, byte[] mergedBytes, CancellationToken cancellationToken)
    {
        var blobKey = BlobKeys.GeneratedCatalog(companyId);

        using var mergedStream = new MemoryStream(mergedBytes);
        await _blobStorageService.UploadAsync(blobKey, mergedStream, "application/pdf", cancellationToken);

        return blobKey;
    }

    private async Task<GeneratedCatalog> CreateCatalogAsync(
        Guid catalogId,
        GenerateCatalogCommand request,
        Company company,
        IReadOnlyList<Item> includedItems,
        List<CatalogIndexEntry> indexSnapshot,
        string generatedBlobKey,
        string fingerprint,
        int rasterizedPageCount,
        double? firstPageAspectRatio,
        CancellationToken cancellationToken)
    {
        var user = await _session.GetAsync<User>(request.UserId, cancellationToken);

        var itemsSnapshot = includedItems
            .Select(p => new CatalogItemSnapshot(p.Id, p.Name, p.Code))
            .ToList();
        var snapshot = new CatalogSnapshot(itemsSnapshot, indexSnapshot);

        var catalog = new GeneratedCatalog
        {
            Company = company,
            User = user,
            GeneratedAt = DateTime.UtcNow,
            GeneratedPdfBlobKey = generatedBlobKey,
            ItemsSnapshotJson = JsonSerializer.Serialize(snapshot),
            ContentFingerprint = fingerprint,
            RasterizedPageCount = rasterizedPageCount,
            PagesPublic = PagesArePublic,
            FirstPageAspectRatio = firstPageAspectRatio
        };

        using var transaction = _session.BeginTransaction();
        await _session.SaveAsync(catalog, catalogId, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return catalog;
    }

    private async Task<GenerateCatalogResult> RefreshSnapshotOnlyAsync(
        GenerateCatalogCommand request,
        GeneratedCatalog existingCatalog,
        IReadOnlyList<MergeEntry> entries,
        CancellationToken cancellationToken)
    {
        var existingSnapshot = JsonSerializer.Deserialize<CatalogSnapshot>(existingCatalog.ItemsSnapshotJson)
            ?? new CatalogSnapshot([], []);
        var pageCounts = ReconstructPageCounts(existingSnapshot.IndexEntries, existingCatalog.RasterizedPageCount);

        var indexSnapshot = CatalogMergePlanBuilder.BuildIndexSnapshot(entries, pageCounts, request.ShowPrices);
        var itemsSnapshot = entries.OfType<ItemEntry>()
            .Select(e => new CatalogItemSnapshot(e.Item.Id, e.Item.Name, e.Item.Code))
            .ToList();

        existingCatalog.ItemsSnapshotJson = JsonSerializer.Serialize(new CatalogSnapshot(itemsSnapshot, indexSnapshot));
        existingCatalog.GeneratedAt = DateTime.UtcNow;
        existingCatalog.User = await _session.GetAsync<User>(request.UserId, cancellationToken);
        await _session.UpdateInTransactionAsync(existingCatalog, cancellationToken);

        var url = CatalogShareUrl.Build(_options.PublicBaseUrl, request.CompanyId, existingCatalog.Company.Slug);
        return new GenerateCatalogResult(existingCatalog.Id, url);
    }

    private static List<int> ReconstructPageCounts(IReadOnlyList<CatalogIndexEntry> cachedEntries, int totalPages)
    {
        var pageCounts = new List<int>(cachedEntries.Count);
        for (var i = 0; i < cachedEntries.Count; i++)
        {
            var end = i + 1 < cachedEntries.Count ? cachedEntries[i + 1].StartPage : totalPages + 1;
            var count = end - cachedEntries[i].StartPage;
            if (count > 0)
            {
                pageCounts.Add(count);
            }
        }
        return pageCounts;
    }

    private async Task DeleteCatalogAsync(Guid companyId, Guid catalogId, CancellationToken cancellationToken)
    {
        var old = await _session.GetAsync<GeneratedCatalog>(catalogId, cancellationToken);
        if (old is null)
        {
            return;
        }

        using var transaction = _session.BeginTransaction();
        await _session.DeleteAsync(old, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        await _blobStorageService.DeleteAsync(old.GeneratedPdfBlobKey, cancellationToken);
        var oldPageStorageService = old.PagesPublic ? (IBlobStorageService)_publicCatalogPageStorageService : _blobStorageService;
        await Parallel.ForEachAsync(
            Enumerable.Range(1, old.RasterizedPageCount),
            new ParallelOptions { MaxDegreeOfParallelism = MaxConcurrentDeletes, CancellationToken = cancellationToken },
            async (pageNumber, ct) =>
            {
                await oldPageStorageService.DeleteAsync(BlobKeys.GeneratedCatalogPage(companyId, catalogId, pageNumber), ct);
                await oldPageStorageService.DeleteAsync(BlobKeys.GeneratedCatalogPageHd(companyId, catalogId, pageNumber), ct);
            });
    }

    private const int MaxConcurrentDeletes = 8;

    private async Task<byte[]> DownloadBytesAsync(string blobKey, CancellationToken cancellationToken)
    {
        await using var stream = await _blobStorageService.DownloadAsync(blobKey, cancellationToken);
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream, cancellationToken);
        return memoryStream.ToArray();
    }
}
