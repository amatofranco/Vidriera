using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Vidriera.Application.Abstractions;
using Vidriera.Application.Subscriptions;

namespace Vidriera.Infrastructure.MercadoPago;

public class MercadoPagoClient : IMercadoPagoClient
{
    private readonly HttpClient _httpClient;
    private readonly MercadoPagoOptions _options;

    public MercadoPagoClient(HttpClient httpClient, IOptions<MercadoPagoOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<MercadoPagoPreapproval> CreatePreapprovalAsync(
        string payerEmail,
        string externalReference,
        decimal amountArs,
        CancellationToken cancellationToken,
        DateTime? startDate = null)
    {
        var request = new CreatePreapprovalRequest(
            "Suscripción Vidriera",
            externalReference,
            payerEmail,
            _options.BackUrl,
            _options.NotificationUrl,
            new AutoRecurringRequest(1, "months", amountArs, _options.CurrencyId, startDate.HasValue ? ToMercadoPagoDate(startDate.Value) : null));

        var response = await _httpClient.PostAsJsonAsync("preapproval", request, cancellationToken);
        var result = await ReadOrThrowAsync<PreapprovalResponse>(response, cancellationToken);

        return ToPreapproval(result);
    }

    public async Task<MercadoPagoPreapproval> GetPreapprovalAsync(string preapprovalId, CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync($"preapproval/{preapprovalId}", cancellationToken);
        var result = await ReadOrThrowAsync<PreapprovalResponse>(response, cancellationToken);

        return ToPreapproval(result);
    }

    public async Task<MercadoPagoPreapproval> CancelPreapprovalAsync(string preapprovalId, CancellationToken cancellationToken)
    {
        var request = new UpdatePreapprovalStatusRequest("cancelled");
        var response = await _httpClient.PutAsJsonAsync($"preapproval/{preapprovalId}", request, cancellationToken);
        var result = await ReadOrThrowAsync<PreapprovalResponse>(response, cancellationToken);

        return ToPreapproval(result);
    }

    public async Task<MercadoPagoPreapproval> ScheduleEndDateAsync(string preapprovalId, DateTime endDate, CancellationToken cancellationToken)
    {
        var request = new UpdatePreapprovalAutoRecurringRequest(new EndDateOnlyAutoRecurring(ToMercadoPagoDate(endDate)));
        var response = await _httpClient.PutAsJsonAsync($"preapproval/{preapprovalId}", request, cancellationToken);
        var result = await ReadOrThrowAsync<PreapprovalResponse>(response, cancellationToken);

        return ToPreapproval(result);
    }

    public async Task<MercadoPagoPayment> GetPaymentAsync(string paymentId, CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync($"v1/payments/{paymentId}", cancellationToken);
        var result = await ReadOrThrowAsync<PaymentResponse>(response, cancellationToken);

        return new MercadoPagoPayment(result.Id.ToString(), result.Status, result.ExternalReference);
    }

    public async Task<IReadOnlyList<MercadoPagoPayment>> SearchPaymentsByExternalReferenceAsync(
        string externalReference,
        CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync(
            $"v1/payments/search?external_reference={Uri.EscapeDataString(externalReference)}&sort=date_created&criteria=desc",
            cancellationToken);
        var result = await ReadOrThrowAsync<PaymentSearchResponse>(response, cancellationToken);

        return result.Results
            .Select(r => new MercadoPagoPayment(r.Id.ToString(), r.Status, r.ExternalReference))
            .ToList();
    }

    private static MercadoPagoPreapproval ToPreapproval(PreapprovalResponse result) =>
        new(result.Id, result.Status, result.InitPoint ?? string.Empty, result.AutoRecurring?.StartDate, result.AutoRecurring?.EndDate);

    private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new(System.Text.Json.JsonSerializerDefaults.Web);

    private static string ToMercadoPagoDate(DateTime date) =>
        new DateTimeOffset(DateTime.SpecifyKind(date, DateTimeKind.Utc)).ToString("yyyy-MM-ddTHH:mm:ss.fffzzz");

    private static async Task<T> ReadOrThrowAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"MercadoPago devolvió {(int)response.StatusCode} {response.StatusCode}: {body}");
        }

        return System.Text.Json.JsonSerializer.Deserialize<T>(body, JsonOptions)
            ?? throw new InvalidOperationException("MercadoPago no devolvió una respuesta válida.");
    }

    private record CreatePreapprovalRequest(
        string Reason,
        [property: JsonPropertyName("external_reference")] string ExternalReference,
        [property: JsonPropertyName("payer_email")] string PayerEmail,
        [property: JsonPropertyName("back_url")] string BackUrl,
        [property: JsonPropertyName("notification_url")] string NotificationUrl,
        [property: JsonPropertyName("auto_recurring")] AutoRecurringRequest AutoRecurring);

    private record AutoRecurringRequest(
        int Frequency,
        [property: JsonPropertyName("frequency_type")] string FrequencyType,
        [property: JsonPropertyName("transaction_amount")] decimal TransactionAmount,
        [property: JsonPropertyName("currency_id")] string CurrencyId,
        [property: JsonPropertyName("start_date")] string? StartDate = null);

    private record UpdatePreapprovalStatusRequest([property: JsonPropertyName("status")] string Status);

    private record UpdatePreapprovalAutoRecurringRequest(
        [property: JsonPropertyName("auto_recurring")] EndDateOnlyAutoRecurring AutoRecurring);

    private record EndDateOnlyAutoRecurring([property: JsonPropertyName("end_date")] string EndDate);

    private record PreapprovalResponse(
        string Id,
        string Status,
        [property: JsonPropertyName("init_point")] string? InitPoint,
        [property: JsonPropertyName("auto_recurring")] AutoRecurringResponse? AutoRecurring);

    private record AutoRecurringResponse(
        [property: JsonPropertyName("start_date")] DateTimeOffset? StartDate,
        [property: JsonPropertyName("end_date")] DateTimeOffset? EndDate);

    private record PaymentResponse(
        long Id,
        string Status,
        [property: JsonPropertyName("external_reference")] string? ExternalReference);

    private record PaymentSearchResponse([property: JsonPropertyName("results")] List<PaymentResponse> Results);
}
