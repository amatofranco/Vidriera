namespace Vidriera.Application.Abstractions;

public record MercadoPagoPreapproval(string Id, string Status, string InitPoint);

public record MercadoPagoPayment(string Id, string Status, string? ExternalReference);

public interface IMercadoPagoClient
{
    Task<MercadoPagoPreapproval> CreatePreapprovalAsync(
        string payerEmail,
        string externalReference,
        decimal amountArs,
        CancellationToken cancellationToken,
        DateTime? startDate = null);

    Task<MercadoPagoPreapproval> GetPreapprovalAsync(string preapprovalId, CancellationToken cancellationToken);

    Task<MercadoPagoPreapproval> CancelPreapprovalAsync(string preapprovalId, CancellationToken cancellationToken);

    Task<MercadoPagoPreapproval> ScheduleEndDateAsync(string preapprovalId, DateTime endDate, CancellationToken cancellationToken);

    Task<MercadoPagoPayment> GetPaymentAsync(string paymentId, CancellationToken cancellationToken);

    Task<IReadOnlyList<MercadoPagoPayment>> SearchPaymentsByExternalReferenceAsync(string externalReference, CancellationToken cancellationToken);
}
