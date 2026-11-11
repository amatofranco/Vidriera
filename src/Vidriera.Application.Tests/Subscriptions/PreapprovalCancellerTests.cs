using Vidriera.Application.Abstractions;
using Vidriera.Application.Subscriptions;

namespace Vidriera.Application.Tests.Subscriptions;

public class PreapprovalCancellerTests
{
    [Theory]
    [InlineData("pending")]
    [InlineData("authorized")]
    [InlineData("paused")]
    public async Task NonFinalPreapproval_IsCancelledInMercadoPago(string status)
    {
        var client = new FakeMercadoPagoClient();

        var result = await PreapprovalCanceller.CancelIfNotFinalAsync(client, new MercadoPagoPreapproval("p1", status, ""), CancellationToken.None);

        Assert.Equal(["p1"], client.CancelledIds);
        Assert.Equal("cancelled", result.Status);
    }

    [Theory]
    [InlineData("cancelled")]
    [InlineData("finished")]
    public async Task FinalPreapproval_IsNotCancelledAgain(string status)
    {
        var client = new FakeMercadoPagoClient();

        var result = await PreapprovalCanceller.CancelIfNotFinalAsync(client, new MercadoPagoPreapproval("p1", status, ""), CancellationToken.None);

        Assert.Empty(client.CancelledIds);
        Assert.Equal(status, result.Status);
    }

    private sealed class FakeMercadoPagoClient : IMercadoPagoClient
    {
        public List<string> CancelledIds { get; } = [];

        public Task<MercadoPagoPreapproval> CancelPreapprovalAsync(string preapprovalId, CancellationToken cancellationToken)
        {
            CancelledIds.Add(preapprovalId);
            return Task.FromResult(new MercadoPagoPreapproval(preapprovalId, "cancelled", ""));
        }

        public Task<MercadoPagoPreapproval> CreatePreapprovalAsync(string payerEmail, string externalReference, decimal amountArs, CancellationToken cancellationToken, DateTime? startDate = null, DateTime? endDate = null) =>
            throw new NotSupportedException();

        public Task<MercadoPagoPreapproval> GetPreapprovalAsync(string preapprovalId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<MercadoPagoPayment> GetPaymentAsync(string paymentId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<MercadoPagoPayment>> SearchPaymentsByExternalReferenceAsync(string externalReference, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
