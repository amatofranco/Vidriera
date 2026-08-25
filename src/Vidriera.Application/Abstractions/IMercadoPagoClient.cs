namespace Vidriera.Application.Abstractions;

public record MercadoPagoPreapproval(string Id, string Status, string InitPoint);

public record MercadoPagoPayment(string Id, string Status, string? ExternalReference);

public interface IMercadoPagoClient
{
    Task<MercadoPagoPreapproval> CreatePreapprovalAsync(
        string payerEmail,
        string externalReference,
        decimal amountArs,
        CancellationToken cancellationToken);

    Task<MercadoPagoPreapproval> GetPreapprovalAsync(string preapprovalId, CancellationToken cancellationToken);

    Task<MercadoPagoPayment> GetPaymentAsync(string paymentId, CancellationToken cancellationToken);

    Task<IReadOnlyList<MercadoPagoPayment>> SearchPaymentsByExternalReferenceAsync(string externalReference, CancellationToken cancellationToken);
}
