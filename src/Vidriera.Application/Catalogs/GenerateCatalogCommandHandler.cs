using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Options;
using NHibernate;
using NHibernate.Linq;
using Vidriera.Application.Abstractions;
using Vidriera.Application.Common;
using Vidriera.Application.Common.Exceptions;
using Vidriera.Domain.Entities;
using Vidriera.Domain.Enums;

namespace Vidriera.Application.Catalogs;

public class GenerateCatalogCommandHandler : IRequestHandler<GenerateCatalogCommand, GenerateCatalogResult>
{
    private const int MaxCatalogsPerCompany = 10;

    private readonly ISession _session;
    private readonly IBlobStorageService _blobStorageService;
    private readonly IPdfMergeService _pdfMergeService;
    private readonly CatalogOptions _options;

    public GenerateCatalogCommandHandler(
        ISession session,
        IBlobStorageService blobStorageService,
        IPdfMergeService pdfMergeService,
        IOptions<CatalogOptions> options)
    {
        _session = session;
        _blobStorageService = blobStorageService;
        _pdfMergeService = pdfMergeService;
        _options = options.Value;
    }

    public async Task<GenerateCatalogResult> Handle(GenerateCatalogCommand request, CancellationToken cancellationToken)
    {
        if (request.ProductIds.Count == 0)
        {
            throw new ValidationException(ErrorMessages.MustSelectAtLeastOneProduct);
        }

        var (allProducts, allSections) = await LoadCatalogDataAsync(request.CompanyId, cancellationToken);
        ValidateSelection(allProducts, request.ProductIds);

        var selectedIds = new HashSet<Guid>(request.ProductIds);
        var topLevel = BuildTopLevelSequence(allSections, allProducts);
        var entries = BuildMergeEntries(topLevel, allProducts, selectedIds);
        var mergePlan = await BuildMergePlanAsync(entries, cancellationToken);

        var mergeResult = await _pdfMergeService.MergeAsync(mergePlan.PdfBytes, cancellationToken);
        var sectionsSnapshot = BuildSectionsSnapshot(mergeResult.PageCounts, mergePlan.CoverMarkers);

        var generatedBlobKey = await UploadMergedPdfAsync(request.CompanyId, mergeResult.Bytes, cancellationToken);
        var catalog = await CreateCatalogAsync(request, mergePlan.IncludedProducts, sectionsSnapshot, generatedBlobKey, cancellationToken);

        await PruneOldCatalogsAsync(request.CompanyId, cancellationToken);

        var url = $"{_options.PublicBaseUrl.TrimEnd('/')}/api/catalogs/{catalog.Id}";
        return new GenerateCatalogResult(catalog.Id, url, catalog.ExpiresAt);
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

    private static void ValidateSelection(IReadOnlyList<Product> allProducts, IReadOnlyList<Guid> productIds)
    {
        var productsById = allProducts.ToDictionary(p => p.Id);

        foreach (var productId in productIds)
        {
            if (!productsById.TryGetValue(productId, out var product))
            {
                throw new ValidationException(ErrorMessages.ProductNotSelectable(productId));
            }

            if (string.IsNullOrEmpty(product.SheetPdfBlobKey))
            {
                throw new ValidationException(ErrorMessages.ProductSheetMissing(product.Name));
            }
        }
    }

    private static IEnumerable<object> BuildTopLevelSequence(IReadOnlyList<Section> sections, IReadOnlyList<Product> allProducts)
    {
        var looseProducts = allProducts.Where(p => p.Section is null);
        return sections.Cast<object>()
            .Concat(looseProducts.Cast<object>())
            .OrderBy(item => item switch
            {
                Section s => s.SortOrder,
                Product p => p.SortOrder,
                _ => 0
            });
    }

    private abstract record MergeEntry;
    private sealed record SectionCoverEntry(Section Section) : MergeEntry;
    private sealed record ProductEntry(Product Product) : MergeEntry;

    private static IEnumerable<MergeEntry> BuildMergeEntries(
        IEnumerable<object> topLevel,
        IReadOnlyList<Product> allProducts,
        HashSet<Guid> selectedIds)
    {
        foreach (var item in topLevel)
        {
            switch (item)
            {
                case Section section:
                    foreach (var entry in BuildSectionEntries(section, allProducts, selectedIds))
                    {
                        yield return entry;
                    }
                    break;

                case Product product when selectedIds.Contains(product.Id):
                    yield return new ProductEntry(product);
                    break;
            }
        }
    }

    private static IEnumerable<MergeEntry> BuildSectionEntries(
        Section section,
        IReadOnlyList<Product> allProducts,
        HashSet<Guid> selectedIds)
    {
        var members = allProducts
            .Where(p => p.Section?.Id == section.Id && selectedIds.Contains(p.Id))
            .OrderBy(p => p.SortOrder)
            .ToList();

        if (members.Count == 0)
        {
            yield break;
        }

        yield return new SectionCoverEntry(section);
        foreach (var member in members)
        {
            yield return new ProductEntry(member);
        }
    }

    private sealed record MergePlan(List<byte[]> PdfBytes, List<Section?> CoverMarkers, List<Product> IncludedProducts);

    private async Task<MergePlan> BuildMergePlanAsync(IEnumerable<MergeEntry> entries, CancellationToken cancellationToken)
    {
        var pdfBytesInOrder = new List<byte[]>();
        var coverMarkers = new List<Section?>();
        var includedProducts = new List<Product>();

        foreach (var entry in entries)
        {
            switch (entry)
            {
                case SectionCoverEntry cover:
                    pdfBytesInOrder.Add(await DownloadBytesAsync(cover.Section.CoverPdfBlobKey!, cancellationToken));
                    coverMarkers.Add(cover.Section);
                    break;

                case ProductEntry productEntry:
                    pdfBytesInOrder.Add(await DownloadBytesAsync(productEntry.Product.SheetPdfBlobKey!, cancellationToken));
                    coverMarkers.Add(null);
                    includedProducts.Add(productEntry.Product);
                    break;
            }
        }

        return new MergePlan(pdfBytesInOrder, coverMarkers, includedProducts);
    }

    private static List<CatalogSectionSnapshot> BuildSectionsSnapshot(
        IReadOnlyList<int> pageCounts,
        IReadOnlyList<Section?> coverMarkers)
    {
        var sectionsSnapshot = new List<CatalogSectionSnapshot>();
        var pageCursor = 0;

        for (var i = 0; i < pageCounts.Count; i++)
        {
            if (coverMarkers[i] is { } coverSection)
            {
                sectionsSnapshot.Add(new CatalogSectionSnapshot(coverSection.Name, pageCursor + 1));
            }
            pageCursor += pageCounts[i];
        }

        return sectionsSnapshot;
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
        IReadOnlyList<Product> includedProducts,
        List<CatalogSectionSnapshot> sectionsSnapshot,
        string generatedBlobKey,
        CancellationToken cancellationToken)
    {
        var user = await _session.GetAsync<User>(request.UserId, cancellationToken);
        var company = await _session.GetAsync<Company>(request.CompanyId, cancellationToken);

        var now = DateTime.UtcNow;
        var expiresAt = now.AddDays(_options.ExpirationDays);

        var productsSnapshot = includedProducts
            .Select(p => new CatalogProductSnapshot(p.Id, p.Name, p.Code))
            .ToList();
        var snapshot = new CatalogSnapshot(productsSnapshot, sectionsSnapshot);

        var catalog = new GeneratedCatalog
        {
            Company = company,
            User = user,
            GeneratedAt = now,
            GeneratedPdfBlobKey = generatedBlobKey,
            ExpiresAt = expiresAt,
            Status = CatalogStatus.Active,
            ProductsSnapshotJson = JsonSerializer.Serialize(snapshot)
        };

        await _session.SaveInTransactionAsync(catalog, cancellationToken);
        return catalog;
    }

    private async Task PruneOldCatalogsAsync(Guid companyId, CancellationToken cancellationToken)
    {
        var catalogs = await _session.Query<GeneratedCatalog>()
            .Where(c => c.Company.Id == companyId)
            .OrderBy(c => c.GeneratedAt)
            .ToListAsync(cancellationToken);

        var excess = catalogs.Count - MaxCatalogsPerCompany;
        if (excess <= 0)
        {
            return;
        }

        var toDelete = catalogs.Take(excess).ToList();

        using var pruneTransaction = _session.BeginTransaction();
        foreach (var old in toDelete)
        {
            await _session.DeleteAsync(old, cancellationToken);
        }
        await pruneTransaction.CommitAsync(cancellationToken);

        foreach (var old in toDelete)
        {
            await _blobStorageService.DeleteAsync(old.GeneratedPdfBlobKey, cancellationToken);
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
