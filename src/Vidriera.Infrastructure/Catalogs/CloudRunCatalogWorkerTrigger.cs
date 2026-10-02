using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vidriera.Application.Abstractions;

namespace Vidriera.Infrastructure.Catalogs;

public class CloudRunCatalogWorkerTrigger : ICatalogWorkerTrigger
{
    public const string HttpClientName = "CatalogWorker";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly CatalogWorkerOptions _options;
    private readonly ILogger<CloudRunCatalogWorkerTrigger> _logger;

    public CloudRunCatalogWorkerTrigger(
        IHttpClientFactory httpClientFactory,
        IOptions<CatalogWorkerOptions> options,
        ILogger<CloudRunCatalogWorkerTrigger> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public Task TriggerAsync(Guid jobId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_options.WorkerUrl))
        {
            return Task.CompletedTask;
        }

        // Deliberadamente no se espera esta llamada ni se le pasa el cancellationToken del
        // request: el disparo debe sobrevivir a que Render termine de responder el POST de
        // encolado. El worker local (con ventana de gracia) es el respaldo si esto falla.
        _ = FireAndForgetAsync(jobId);

        return Task.CompletedTask;
    }

    private async Task FireAndForgetAsync(Guid jobId)
    {
        try
        {
            using var client = _httpClientFactory.CreateClient(HttpClientName);
            using var response = await client.PostAsync($"{jobId}/process", null, CancellationToken.None);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "El worker externo devolvió {StatusCode} para el job {JobId}; el worker local lo tomará como respaldo.",
                    response.StatusCode, jobId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex, "No se pudo disparar el worker externo para el job {JobId}; el worker local lo tomará como respaldo.", jobId);
        }
    }
}
