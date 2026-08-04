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
            throw new ValidationException("Hay que seleccionar al menos un producto.");
        }

        // The requested ids are just the "which ones are checked" set -- the actual merge
        // order is rebuilt here from each item's own SortOrder (already kept in sync by
        // drag-and-drop/the reorder endpoints), not trusted from the request itself.
        var allProducts = await _session.Query<Product>()
            .Where(p => p.Company.Id == request.CompanyId)
            .ToListAsync(cancellationToken);
        var allSections = await _session.Query<Section>()
            .Where(s => s.Company.Id == request.CompanyId)
            .ToListAsync(cancellationToken);

        var productsById = allProducts.ToDictionary(p => p.Id);
        var selectedIds = new HashSet<Guid>(request.ProductIds);

        foreach (var productId in request.ProductIds)
        {
            if (!productsById.TryGetValue(productId, out var product))
            {
                throw new ValidationException($"El producto {productId} no existe o no pertenece a esta empresa.");
            }

            if (string.IsNullOrEmpty(product.SheetPdfBlobKey))
            {
                throw new ValidationException($"El producto '{product.Name}' todavía no tiene la hoja PDF cargada.");
            }
        }

        var looseProducts = allProducts.Where(p => p.Section is null);
        var topLevel = allSections.Cast<object>()
            .Concat(looseProducts.Cast<object>())
            .OrderBy(item => item switch
            {
                Section s => s.SortOrder,
                Product p => p.SortOrder,
                _ => 0
            });

        var pdfBytesInOrder = new List<byte[]>();
        // Parallel to pdfBytesInOrder -- non-null at the index where a section's own cover
        // page was added, so the page offsets returned by the merge can be matched back to
        // "which section starts here" without re-walking topLevel a second time.
        var coverMarkers = new List<Section?>();
        var includedProducts = new List<Product>();

        foreach (var item in topLevel)
        {
            if (item is Section section)
            {
                var members = allProducts
                    .Where(p => p.Section?.Id == section.Id && selectedIds.Contains(p.Id))
                    .OrderBy(p => p.SortOrder)
                    .ToList();

                if (members.Count == 0)
                {
                    continue;
                }

                pdfBytesInOrder.Add(await DownloadBytesAsync(section.CoverPdfBlobKey!, cancellationToken));
                coverMarkers.Add(section);
                foreach (var member in members)
                {
                    pdfBytesInOrder.Add(await DownloadBytesAsync(member.SheetPdfBlobKey!, cancellationToken));
                    coverMarkers.Add(null);
                    includedProducts.Add(member);
                }
            }
            else if (item is Product product && selectedIds.Contains(product.Id))
            {
                pdfBytesInOrder.Add(await DownloadBytesAsync(product.SheetPdfBlobKey!, cancellationToken));
                coverMarkers.Add(null);
                includedProducts.Add(product);
            }
        }

        var mergeResult = await _pdfMergeService.MergeAsync(pdfBytesInOrder, cancellationToken);

        var sectionsSnapshot = new List<CatalogSectionSnapshot>();
        var pageCursor = 0;
        for (var i = 0; i < mergeResult.PageCounts.Count; i++)
        {
            if (coverMarkers[i] is { } coverSection)
            {
                sectionsSnapshot.Add(new CatalogSectionSnapshot(coverSection.Name, pageCursor + 1));
            }
            pageCursor += mergeResult.PageCounts[i];
        }

        var generatedBlobKey = BlobKeys.GeneratedCatalog(request.CompanyId);

        using (var mergedStream = new MemoryStream(mergeResult.Bytes))
        {
            await _blobStorageService.UploadAsync(generatedBlobKey, mergedStream, "application/pdf", cancellationToken);
        }

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

        using var transaction = _session.BeginTransaction();
        await _session.SaveAsync(catalog, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        await PruneOldCatalogsAsync(request.CompanyId, cancellationToken);

        var url = $"{_options.PublicBaseUrl.TrimEnd('/')}/api/catalogs/{catalog.Id}";
        return new GenerateCatalogResult(catalog.Id, url, expiresAt);
    }

    private const int MaxCatalogsPerCompany = 10;

    // Keeps the history bounded: past the 10 most recent catalogs, the oldest ones are deleted
    // outright (row + R2 blob), not just hidden -- a link that old stops working, by design.
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
