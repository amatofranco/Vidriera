using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Vidriera.Application.Abstractions;

namespace Vidriera.Infrastructure.ExchangeRate;

public class DolarApiExchangeRateService : IExchangeRateService
{
    private readonly HttpClient _httpClient;

    public DolarApiExchangeRateService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<decimal> GetUsdToArsOficialRateAsync(CancellationToken cancellationToken)
    {
        var result = await _httpClient.GetFromJsonAsync<DolarOficialResponse>("v1/dolares/oficial", cancellationToken)
            ?? throw new InvalidOperationException("No se pudo obtener la cotización del dólar oficial.");

        return result.Venta;
    }

    private record DolarOficialResponse([property: JsonPropertyName("venta")] decimal Venta);
}
