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
        CancellationToken cancellationToken)
    {
        var request = new CreatePreapprovalRequest(
            "Suscripción Vidriera",
            externalReference,
            payerEmail,
            _options.BackUrl,
            _options.NotificationUrl,
            new AutoRecurringRequest(1, "months", amountArs, _options.CurrencyId));

        var response = await _httpClient.PostAsJsonAsync("preapproval", request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<PreapprovalResponse>(cancellationToken)
            ?? throw new InvalidOperationException("MercadoPago no devolvió una respuesta válida al crear la preapproval.");

        return new MercadoPagoPreapproval(result.Id, result.Status, result.InitPoint ?? string.Empty);
    }

    public async Task<MercadoPagoPreapproval> GetPreapprovalAsync(string preapprovalId, CancellationToken cancellationToken)
    {
        var result = await _httpClient.GetFromJsonAsync<PreapprovalResponse>($"preapproval/{preapprovalId}", cancellationToken)
            ?? throw new InvalidOperationException($"MercadoPago no devolvió datos para la preapproval {preapprovalId}.");

        return new MercadoPagoPreapproval(result.Id, result.Status, result.InitPoint ?? string.Empty);
    }

    public async Task<MercadoPagoPayment> GetPaymentAsync(string paymentId, CancellationToken cancellationToken)
    {
        var result = await _httpClient.GetFromJsonAsync<PaymentResponse>($"v1/payments/{paymentId}", cancellationToken)
            ?? throw new InvalidOperationException($"MercadoPago no devolvió datos para el pago {paymentId}.");

        return new MercadoPagoPayment(result.Id, result.Status, result.ExternalReference);
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
        [property: JsonPropertyName("currency_id")] string CurrencyId);

    private record PreapprovalResponse(
        string Id,
        string Status,
        [property: JsonPropertyName("init_point")] string? InitPoint);

    private record PaymentResponse(
        string Id,
        string Status,
        [property: JsonPropertyName("external_reference")] string? ExternalReference);
}
