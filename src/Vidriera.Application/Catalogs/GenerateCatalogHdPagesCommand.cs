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
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(5);

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

        var generator = new CatalogHdPagesGenerator(
            pageStorage,
            _rasterizerService,
            _generationGate,
            ct => IsCurrentCatalogAsync(companyId, catalogId, ct),
            HasPendingJobAsync,
            CheckInterval);

        var result = await generator.GenerateAsync(companyId, catalogId, pdfBytes, catalog.RasterizedPageCount, cancellationToken);

        _logger.LogInformation(
            "Versión en alta del catálogo {CatalogId}: {Outcome}, {Generated}/{Total} páginas en {ElapsedMs} ms.",
            catalogId, DescribeOutcome(result.Outcome), result.GeneratedPages, catalog.RasterizedPageCount, stopwatch.ElapsedMilliseconds);
    }

    private static string DescribeOutcome(HdPagesOutcome outcome) => outcome switch
    {
        HdPagesOutcome.Completed => "generada",
        HdPagesOutcome.YieldedToGeneration => "interrumpida para dejar pasar una generación",
        HdPagesOutcome.CatalogReplaced => "cancelada porque el catálogo fue reemplazado (se borró lo generado)",
        _ => outcome.ToString()
    };

    private async Task<bool> IsCurrentCatalogAsync(Guid companyId, Guid catalogId, CancellationToken cancellationToken)
    {
        var currentCatalogId = await _session.Query<Company>()
            .Where(c => c.Id == companyId)
            .Select(c => c.CurrentCatalogId)
            .FirstOrDefaultAsync(cancellationToken);
        return currentCatalogId == catalogId;
    }

    private Task<bool> HasPendingJobAsync(CancellationToken cancellationToken) =>
        _session.Query<CatalogGenerationJob>()
            .AnyAsync(j => j.Status == CatalogGenerationJobStatus.Pending, cancellationToken);
}
