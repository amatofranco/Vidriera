using System.Text.Json;
using MediatR;
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
    private static readonly TimeSpan GateTimeout = TimeSpan.FromSeconds(3);
    private const int EstimatedPagesPerSectionCover = 2;

    private readonly ISession _session;
    private readonly IBlobStorageService _blobStorageService;
    private readonly IPdfMergeService _pdfMergeService;
    private readonly IPdfRasterizerService _pdfRasterizerService;
    private readonly CatalogGenerationGate _generationGate;
    private readonly CatalogOptions _options;

    public GenerateCatalogCommandHandler(
        ISession session,
        IBlobStorageService blobStorageService,
        IPdfMergeService pdfMergeService,
        IPdfRasterizerService pdfRasterizerService,
        CatalogGenerationGate generationGate,
        IOptions<CatalogOptions> options)
    {
        _session = session;
        _blobStorageService = blobStorageService;
        _pdfMergeService = pdfMergeService;
        _pdfRasterizerService = pdfRasterizerService;
        _generationGate = generationGate;
        _options = options.Value;
    }

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

        if (existingCatalog is not null && existingCatalog.ContentFingerprint == fingerprint)
        {
            return await RefreshSnapshotOnlyAsync(request, existingCatalog, entries, cancellationToken);
        }

        var estimatedPages = EstimateTotalPages(entries);
        if (estimatedPages > _options.MaxTotalPages)
        {
            throw new ValidationException(ErrorMessages.CatalogTooLarge(estimatedPages, _options.MaxTotalPages));
        }

        var mergePlan = await BuildAndMergeAsync(entries, request.OnProgress, cancellationToken);
        var mergeResult = mergePlan.MergeResult;
        var indexSnapshot = CatalogMergePlanBuilder.BuildIndexSnapshot(entries, mergeResult.PageCounts, request.ShowPrices);

        var generatedBlobKey = await UploadMergedPdfAsync(request.CompanyId, mergeResult.Bytes, cancellationToken);
        var previousCatalogId = company.CurrentCatalogId;

        var catalog = await CreateCatalogAsync(request, company, mergePlan.IncludedItems, indexSnapshot, generatedBlobKey, fingerprint, cancellationToken);
        await RasterizePagesAsync(catalog, mergeResult.Bytes, mergeResult.PageCounts.Sum(), request.OnProgress, cancellationToken);

        company.CurrentCatalogId = catalog.Id;
        await _session.UpdateInTransactionAsync(company, cancellationToken);

        if (previousCatalogId.HasValue)
        {
            await DeleteCatalogAsync(request.CompanyId, previousCatalogId.Value, CancellationToken.None);
        }

        var url = CatalogShareUrl.Build(_options.PublicBaseUrl, request.CompanyId, company.Slug);
        return new GenerateCatalogResult(catalog.Id, url);
    }

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
                SectionCoverEntry cover when cover.Section.CoverPdfBlobKey is not null => EstimatedPagesPerSectionCover,
                _ => 0
            };
        }
        return total;
    }

    private sealed record MergePlan(PdfMergeResult MergeResult, List<Item> IncludedItems);

    private const int MaxConcurrentDownloads = 4;

    private async Task<MergePlan> BuildAndMergeAsync(
        IReadOnlyList<MergeEntry> entries,
        Func<CatalogGenerationProgress, Task>? onProgress,
        CancellationToken cancellationToken)
    {
        var physicalEntries = entries
            .Where(entry => entry is ItemEntry || (entry is SectionCoverEntry cover && cover.Section.CoverPdfBlobKey is not null))
            .ToList();

        var includedItems = new List<Item>();
        foreach (var entry in physicalEntries)
        {
            if (entry is ItemEntry itemEntry)
            {
                includedItems.Add(itemEntry.Item);
            }
        }

        using var session = _pdfMergeService.CreateSession();
        var coordinator = new OrderedMergeCoordinator(session);

        var downloadedCount = 0;
        var pendingDownloads = new List<Task>(physicalEntries.Count);
        using var downloadSemaphore = new SemaphoreSlim(MaxConcurrentDownloads);
        using var progressLock = new SemaphoreSlim(1, 1);

        for (var index = 0; index < physicalEntries.Count; index++)
        {
            var blobKey = physicalEntries[index] switch
            {
                SectionCoverEntry cover => cover.Section.CoverPdfBlobKey!,
                ItemEntry itemEntry => itemEntry.Item.SheetPdfBlobKey!,
                _ => throw new InvalidOperationException("Unknown merge entry type.")
            };

            await downloadSemaphore.WaitAsync(cancellationToken);
            pendingDownloads.Add(DownloadAndMergeEntryAsync(
                blobKey,
                index,
                coordinator,
                downloadSemaphore,
                () =>
                {
                    var completed = Interlocked.Increment(ref downloadedCount);
                    return ReportProgressSerializedAsync(progressLock, onProgress, "downloading", completed, physicalEntries.Count);
                },
                cancellationToken));
            pendingDownloads.RemoveAll(t => t.Status == TaskStatus.RanToCompletion);
        }

        await Task.WhenAll(pendingDownloads);

        return new MergePlan(session.Complete(), includedItems);
    }

    private async Task DownloadAndMergeEntryAsync(
        string blobKey,
        int index,
        OrderedMergeCoordinator coordinator,
        SemaphoreSlim downloadSemaphore,
        Func<Task> reportDownloaded,
        CancellationToken cancellationToken)
    {
        try
        {
            var bytes = await DownloadBytesAsync(blobKey, cancellationToken);
            await coordinator.AddAsync(index, bytes, cancellationToken);
            await reportDownloaded();
        }
        finally
        {
            downloadSemaphore.Release();
        }
    }

    private sealed class OrderedMergeCoordinator
    {
        private readonly IPdfMergeSession _session;
        private readonly Dictionary<int, byte[]> _pending = new();
        private readonly SemaphoreSlim _lock = new(1, 1);
        private int _nextIndex;

        public OrderedMergeCoordinator(IPdfMergeSession session)
        {
            _session = session;
        }

        public async Task AddAsync(int index, byte[] pdfBytes, CancellationToken cancellationToken)
        {
            await _lock.WaitAsync(cancellationToken);
            try
            {
                _pending[index] = pdfBytes;
                while (_pending.Remove(_nextIndex, out var nextBytes))
                {
                    _session.AddDocument(nextBytes);
                    _nextIndex++;
                }
            }
            finally
            {
                _lock.Release();
            }
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
        GenerateCatalogCommand request,
        Company company,
        IReadOnlyList<Item> includedItems,
        List<CatalogIndexEntry> indexSnapshot,
        string generatedBlobKey,
        string fingerprint,
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
            ContentFingerprint = fingerprint
        };

        await _session.SaveInTransactionAsync(catalog, cancellationToken);
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

    private const int MaxConcurrentPageUploads = 4;

    private async Task RasterizePagesAsync(
        GeneratedCatalog catalog,
        byte[] mergedPdfBytes,
        int totalPages,
        Func<CatalogGenerationProgress, Task>? onProgress,
        CancellationToken cancellationToken)
    {
        var uploadedPageCount = 0;
        var pendingUploads = new List<Task>();
        using var uploadSemaphore = new SemaphoreSlim(MaxConcurrentPageUploads);
        using var progressLock = new SemaphoreSlim(1, 1);
        try
        {
            var allPageIndices = Enumerable.Range(0, totalPages).ToList();
            await foreach (var (pageIndex, jpegBytes) in _pdfRasterizerService.RasterizePagesToJpegAsync(mergedPdfBytes, allPageIndices, cancellationToken))
            {
                await uploadSemaphore.WaitAsync(cancellationToken);
                pendingUploads.Add(UploadPageAndReportAsync(
                    catalog,
                    pageIndex,
                    jpegBytes,
                    uploadSemaphore,
                    () =>
                    {
                        var completed = Interlocked.Increment(ref uploadedPageCount);
                        return ReportProgressSerializedAsync(progressLock, onProgress, "rasterizing", completed, totalPages);
                    },
                    cancellationToken));
                pendingUploads.RemoveAll(t => t.Status == TaskStatus.RanToCompletion);
            }

            await Task.WhenAll(pendingUploads);

            catalog.RasterizedPageCount = uploadedPageCount;
            await _session.UpdateInTransactionAsync(catalog, cancellationToken);
        }
        catch
        {
            await _session.DeleteInTransactionAsync(catalog, CancellationToken.None);
            await _blobStorageService.DeleteAsync(catalog.GeneratedPdfBlobKey, CancellationToken.None);
            for (var pageNumber = 1; pageNumber <= totalPages; pageNumber++)
            {
                await _blobStorageService.DeleteAsync(
                    BlobKeys.GeneratedCatalogPage(catalog.Company.Id, catalog.Id, pageNumber),
                    CancellationToken.None);
            }
            throw;
        }
    }

    private async Task UploadPageAndReportAsync(
        GeneratedCatalog catalog,
        int pageIndex,
        byte[] jpegBytes,
        SemaphoreSlim uploadSemaphore,
        Func<Task> reportUploaded,
        CancellationToken cancellationToken)
    {
        try
        {
            var blobKey = BlobKeys.GeneratedCatalogPage(catalog.Company.Id, catalog.Id, pageIndex + 1);
            using var stream = new MemoryStream(jpegBytes);
            await _blobStorageService.UploadAsync(blobKey, stream, "image/jpeg", cancellationToken);
            await reportUploaded();
        }
        finally
        {
            uploadSemaphore.Release();
        }
    }

    private static async Task ReportProgressSerializedAsync(
        SemaphoreSlim progressLock,
        Func<CatalogGenerationProgress, Task>? onProgress,
        string stage,
        int current,
        int total)
    {
        if (onProgress is null)
        {
            return;
        }

        await progressLock.WaitAsync();
        try
        {
            await onProgress(new CatalogGenerationProgress(stage, current, total));
        }
        finally
        {
            progressLock.Release();
        }
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
        for (var pageNumber = 1; pageNumber <= old.RasterizedPageCount; pageNumber++)
        {
            await _blobStorageService.DeleteAsync(
                BlobKeys.GeneratedCatalogPage(companyId, catalogId, pageNumber),
                cancellationToken);
        }
    }

    private async Task<byte[]> DownloadBytesAsync(string blobKey, CancellationToken cancellationToken)
    {
        await using var stream = await _blobStorageService.DownloadAsync(blobKey, cancellationToken);
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream, cancellationToken);
        return memoryStream.ToArray();
    }
}
