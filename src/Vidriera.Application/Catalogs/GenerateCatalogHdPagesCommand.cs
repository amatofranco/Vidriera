using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using NHibernate;
using NHibernate.Linq;
using Vidriera.Application.Abstractions;
using Vidriera.Application.Common;
using Vidriera.Domain.Entities;

namespace Vidriera.Application.Catalogs;

public record GenerateCatalogHdPagesCommand(Guid CatalogId) : IRequest;

public class GenerateCatalogHdPagesCommandHandler : IRequestHandler<GenerateCatalogHdPagesCommand>
{
    private static readonly TimeSpan PendingJobCheckInterval = TimeSpan.FromSeconds(5);

    private readonly ISession _session;
    private readonly IBlobStorageService _blobStorageService;
    private readonly IPublicCatalogPageStorageService _publicCatalogPageStorageService;
    private readonly IPdfRasterizerService _rasterizerService;
    private readonly CatalogGenerationGate _generationGate;
    private readonly ILogger<GenerateCatalogHdPagesCommandHandler> _logger;

    public GenerateCatalogHdPagesCommandHandler(
        ISession session,
        IBlobStorageService blobStorageService,
        IPublicCatalogPageStorageService publicCatalogPageStorageService,
        IPdfRasterizerService rasterizerService,
        CatalogGenerationGate generationGate,
        ILogger<GenerateCatalogHdPagesCommandHandler> logger)
    {
        _session = session;
        _blobStorageService = blobStorageService;
        _publicCatalogPageStorageService = publicCatalogPageStorageService;
        _rasterizerService = rasterizerService;
        _generationGate = generationGate;
        _logger = logger;
    }

    public async Task Handle(GenerateCatalogHdPagesCommand request, CancellationToken cancellationToken)
    {
        if (!await _generationGate.TryEnterAsync(TimeSpan.Zero, cancellationToken))
        {
            return;
        }

        try
        {
            await GenerateAsync(request.CatalogId, cancellationToken);
        }
        finally
        {
            _generationGate.Exit();
        }
    }

    private async Task GenerateAsync(Guid catalogId, CancellationToken cancellationToken)
    {
        var catalog = await _session.GetAsync<GeneratedCatalog>(catalogId, cancellationToken);
        if (catalog is null || catalog.RasterizedPageCount == 0)
        {
            return;
        }

        var companyId = catalog.Company.Id;
        var pageStorage = catalog.PagesPublic ? (IBlobStorageService)_publicCatalogPageStorageService : _blobStorageService;

        var lastPageKey = BlobKeys.GeneratedCatalogPageHd(companyId, catalogId, catalog.RasterizedPageCount);
        if (await pageStorage.ExistsAsync(lastPageKey, cancellationToken))
        {
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        byte[] pdfBytes;
        await using (var stream = await _blobStorageService.DownloadAsync(catalog.GeneratedPdfBlobKey, cancellationToken))
        using (var memory = new MemoryStream())
        {
            await stream.CopyToAsync(memory, cancellationToken);
            pdfBytes = memory.ToArray();
        }

        var generated = 0;
        var lastPendingJobCheck = Stopwatch.StartNew();
        var pageIndices = Enumerable.Range(0, catalog.RasterizedPageCount).ToList();

        await foreach (var (pageIndex, imageBytes, _, _) in _rasterizerService.RasterizePagesToHdImagesAsync(pdfBytes, pageIndices, cancellationToken))
        {
            using (var imageStream = new MemoryStream(imageBytes))
            {
                await pageStorage.UploadAsync(
                    BlobKeys.GeneratedCatalogPageHd(companyId, catalogId, pageIndex + 1), imageStream, "image/webp", cancellationToken);
            }
            generated++;

            if (await ShouldYieldAsync(lastPendingJobCheck, cancellationToken))
            {
                _logger.LogInformation(
                    "Versión en alta del catálogo {CatalogId} interrumpida para dejar pasar una generación: {Generated}/{Total} páginas en {ElapsedMs} ms.",
                    catalogId, generated, catalog.RasterizedPageCount, stopwatch.ElapsedMilliseconds);
                return;
            }
        }

        _logger.LogInformation(
            "Versión en alta del catálogo {CatalogId} generada: {Total} páginas en {ElapsedMs} ms.",
            catalogId, generated, stopwatch.ElapsedMilliseconds);
    }

    private async Task<bool> ShouldYieldAsync(Stopwatch lastPendingJobCheck, CancellationToken cancellationToken)
    {
        if (_generationGate.HasWaiters)
        {
            return true;
        }

        if (lastPendingJobCheck.Elapsed < PendingJobCheckInterval)
        {
            return false;
        }

        lastPendingJobCheck.Restart();
        return await _session.Query<CatalogGenerationJob>()
            .AnyAsync(j => j.Status == CatalogGenerationJobStatus.Pending, cancellationToken);
    }
}
