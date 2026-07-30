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

        var products = await _session.Query<Product>()
            .Where(p => p.Company.Id == request.CompanyId && request.ProductIds.Contains(p.Id))
            .ToListAsync(cancellationToken);

        var productsById = products.ToDictionary(p => p.Id);

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

        var pdfBytesInOrder = new List<byte[]>();
        foreach (var productId in request.ProductIds)
        {
            var product = productsById[productId];
            await using var stream = await _blobStorageService.DownloadAsync(product.SheetPdfBlobKey!, cancellationToken);
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream, cancellationToken);
            pdfBytesInOrder.Add(memoryStream.ToArray());
        }

        var mergedPdf = await _pdfMergeService.MergeAsync(pdfBytesInOrder, cancellationToken);

        var catalogId = Guid.NewGuid();
        var generatedBlobKey = $"companies/{request.CompanyId}/catalogs/{catalogId}.pdf";

        using (var mergedStream = new MemoryStream(mergedPdf))
        {
            await _blobStorageService.UploadAsync(generatedBlobKey, mergedStream, "application/pdf", cancellationToken);
        }

        var user = await _session.GetAsync<User>(request.UserId, cancellationToken);
        var company = await _session.GetAsync<Company>(request.CompanyId, cancellationToken);

        var now = DateTime.UtcNow;
        var expiresAt = now.AddDays(_options.ExpirationDays);

        var catalog = new GeneratedCatalog
        {
            Id = catalogId,
            Company = company,
            User = user,
            GeneratedAt = now,
            GeneratedPdfBlobKey = generatedBlobKey,
            ExpiresAt = expiresAt,
            Status = CatalogStatus.Active
        };

        foreach (var productId in request.ProductIds)
        {
            catalog.Products.Add(new GeneratedCatalogProduct
            {
                GeneratedCatalog = catalog,
                Product = productsById[productId]
            });
        }

        using var transaction = _session.BeginTransaction();
        await _session.SaveAsync(catalog, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var url = $"{_options.PublicBaseUrl.TrimEnd('/')}/api/catalogs/{catalogId}";
        return new GenerateCatalogResult(catalogId, url, expiresAt);
    }
}
