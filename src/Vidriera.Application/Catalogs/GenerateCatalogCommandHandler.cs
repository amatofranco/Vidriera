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
    private readonly ISession _session;
    private readonly IBlobStorageService _blobStorageService;
    private readonly IPdfMergeService _pdfMergeService;
    private readonly IPdfRasterizerService _pdfRasterizerService;
    private readonly CatalogOptions _options;

    public GenerateCatalogCommandHandler(
        ISession session,
        IBlobStorageService blobStorageService,
        IPdfMergeService pdfMergeService,
        IPdfRasterizerService pdfRasterizerService,
        IOptions<CatalogOptions> options)
    {
        _session = session;
        _blobStorageService = blobStorageService;
        _pdfMergeService = pdfMergeService;
        _pdfRasterizerService = pdfRasterizerService;
        _options = options.Value;
    }

    public async Task<GenerateCatalogResult> Handle(GenerateCatalogCommand request, CancellationToken cancellationToken)
    {
        var (allProducts, allSections) = await LoadCatalogDataAsync(request.CompanyId, cancellationToken);
        var selectedIds = new HashSet<Guid>(
            allProducts.Where(p => p.HasStock && !string.IsNullOrEmpty(p.SheetPdfBlobKey)).Select(p => p.Id));

        if (selectedIds.Count == 0)
        {
            throw new ValidationException(ErrorMessages.MustSelectAtLeastOneProduct);
        }

        var entries = CatalogMergePlanBuilder.BuildEntries(allSections, allProducts, selectedIds);
        var mergePlan = await BuildMergePlanAsync(entries, request.OnProgress, cancellationToken);

        var mergeResult = await _pdfMergeService.MergeAsync(mergePlan.PdfBytes, cancellationToken);
        var indexSnapshot = CatalogMergePlanBuilder.BuildIndexSnapshot(entries, mergeResult.PageCounts);

        var generatedBlobKey = await UploadMergedPdfAsync(request.CompanyId, mergeResult.Bytes, cancellationToken);
        var company = await _session.GetAsync<Company>(request.CompanyId, cancellationToken);
        var previousCatalogId = company.CurrentCatalogId;

        var catalog = await CreateCatalogAsync(request, company, mergePlan.IncludedProducts, indexSnapshot, generatedBlobKey, cancellationToken);
        await RasterizePagesAsync(catalog, mergeResult.Bytes, mergeResult.PageCounts.Sum(), request.OnProgress, cancellationToken);

        company.CurrentCatalogId = catalog.Id;
        await _session.UpdateInTransactionAsync(company, cancellationToken);

        if (previousCatalogId.HasValue)
        {
            await DeleteCatalogAsync(request.CompanyId, previousCatalogId.Value, CancellationToken.None);
        }

        var url = $"{_options.PublicBaseUrl.TrimEnd('/')}/api/catalogs/company/{request.CompanyId}";
        return new GenerateCatalogResult(catalog.Id, url);
    }

    private async Task<(List<Product> Products, List<Section> Sections)> LoadCatalogDataAsync(
        Guid companyId,
        CancellationToken cancellationToken)
    {
        var products = await _session.Query<Product>()
            .Where(p => p.Company.Id == companyId)
            .ToListAsync(cancellationToken);
        var sections = await _session.Query<Section>()
            .Where(s => s.Company.Id == companyId)
            .ToListAsync(cancellationToken);

        return (products, sections);
    }

    private sealed record MergePlan(List<byte[]> PdfBytes, List<Product> IncludedProducts);

    private const int MaxConcurrentDownloads = 4;

    private async Task<MergePlan> BuildMergePlanAsync(
        IReadOnlyList<MergeEntry> entries,
        Func<CatalogGenerationProgress, Task>? onProgress,
        CancellationToken cancellationToken)
    {
        var pdfBytesInOrder = new byte[entries.Count][];
        var includedProducts = new List<Product>();

        foreach (var entry in entries)
        {
            if (entry is ProductEntry productEntry)
            {
                includedProducts.Add(productEntry.Product);
            }
        }

        var downloadedCount = 0;
        var pendingDownloads = new List<Task>(entries.Count);
        using var downloadSemaphore = new SemaphoreSlim(MaxConcurrentDownloads);
        using var progressLock = new SemaphoreSlim(1, 1);

        for (var index = 0; index < entries.Count; index++)
        {
            var blobKey = entries[index] switch
            {
                SectionCoverEntry cover => cover.Section.CoverPdfBlobKey!,
                ProductEntry productEntry => productEntry.Product.SheetPdfBlobKey!,
                _ => throw new InvalidOperationException("Unknown merge entry type.")
            };

            await downloadSemaphore.WaitAsync(cancellationToken);
            pendingDownloads.Add(DownloadEntryAndReportAsync(
                blobKey,
                index,
                pdfBytesInOrder,
                downloadSemaphore,
                () =>
                {
                    var completed = Interlocked.Increment(ref downloadedCount);
                    return ReportProgressSerializedAsync(progressLock, onProgress, "downloading", completed, entries.Count);
                },
                cancellationToken));
        }

        await Task.WhenAll(pendingDownloads);

        return new MergePlan(pdfBytesInOrder.ToList(), includedProducts);
    }

    private async Task DownloadEntryAndReportAsync(
        string blobKey,
        int index,
        byte[][] results,
        SemaphoreSlim downloadSemaphore,
        Func<Task> reportDownloaded,
        CancellationToken cancellationToken)
    {
        try
        {
            results[index] = await DownloadBytesAsync(blobKey, cancellationToken);
            await reportDownloaded();
        }
        finally
        {
            downloadSemaphore.Release();
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
        IReadOnlyList<Product> includedProducts,
        List<CatalogIndexEntry> indexSnapshot,
        string generatedBlobKey,
        CancellationToken cancellationToken)
    {
        var user = await _session.GetAsync<User>(request.UserId, cancellationToken);

        var productsSnapshot = includedProducts
            .Select(p => new CatalogProductSnapshot(p.Id, p.Name, p.Code))
            .ToList();
        var snapshot = new CatalogSnapshot(productsSnapshot, indexSnapshot);

        var catalog = new GeneratedCatalog
        {
            Company = company,
            User = user,
            GeneratedAt = DateTime.UtcNow,
            GeneratedPdfBlobKey = generatedBlobKey,
            ProductsSnapshotJson = JsonSerializer.Serialize(snapshot)
        };

        await _session.SaveInTransactionAsync(catalog, cancellationToken);
        return catalog;
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
