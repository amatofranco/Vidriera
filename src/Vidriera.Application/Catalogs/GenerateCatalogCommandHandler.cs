using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Options;
using NHibernate;
using NHibernate.Linq;
using Vidriera.Application.Abstractions;
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
                foreach (var member in members)
                {
                    pdfBytesInOrder.Add(await DownloadBytesAsync(member.SheetPdfBlobKey!, cancellationToken));
                    includedProducts.Add(member);
                }
            }
            else if (item is Product product && selectedIds.Contains(product.Id))
            {
                pdfBytesInOrder.Add(await DownloadBytesAsync(product.SheetPdfBlobKey!, cancellationToken));
                includedProducts.Add(product);
            }
        }

        var mergedPdf = await _pdfMergeService.MergeAsync(pdfBytesInOrder, cancellationToken);

        var generatedBlobKey = $"companies/{request.CompanyId}/catalogs/{Guid.NewGuid()}.pdf";

        using (var mergedStream = new MemoryStream(mergedPdf))
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

        var catalog = new GeneratedCatalog
        {
            Company = company,
            User = user,
            GeneratedAt = now,
            GeneratedPdfBlobKey = generatedBlobKey,
            ExpiresAt = expiresAt,
            Status = CatalogStatus.Active,
            ProductsSnapshotJson = JsonSerializer.Serialize(productsSnapshot)
        };

        using var transaction = _session.BeginTransaction();
        await _session.SaveAsync(catalog, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var url = $"{_options.PublicBaseUrl.TrimEnd('/')}/api/catalogs/{catalog.Id}";
        return new GenerateCatalogResult(catalog.Id, url, expiresAt);
    }

    private async Task<byte[]> DownloadBytesAsync(string blobKey, CancellationToken cancellationToken)
    {
        await using var stream = await _blobStorageService.DownloadAsync(blobKey, cancellationToken);
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream, cancellationToken);
        return memoryStream.ToArray();
    }
}
