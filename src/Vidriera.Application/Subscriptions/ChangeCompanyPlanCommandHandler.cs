using MediatR;
using Microsoft.Extensions.Options;
using NHibernate;
using NHibernate.Linq;
using Vidriera.Application.Abstractions;
using Vidriera.Application.Common;
using Vidriera.Application.Common.Exceptions;
using Vidriera.Domain.Entities;

namespace Vidriera.Application.Subscriptions;

public class ChangeCompanyPlanCommandHandler : IRequestHandler<ChangeCompanyPlanCommand, ChangeCompanyPlanResult>
{
    private readonly ISession _session;
    private readonly IMercadoPagoClient _mercadoPagoClient;
    private readonly IExchangeRateService _exchangeRateService;
    private readonly MercadoPagoOptions _options;

    public ChangeCompanyPlanCommandHandler(
        ISession session,
        IMercadoPagoClient mercadoPagoClient,
        IExchangeRateService exchangeRateService,
        IOptions<MercadoPagoOptions> options)
    {
        _session = session;
        _mercadoPagoClient = mercadoPagoClient;
        _exchangeRateService = exchangeRateService;
        _options = options.Value;
    }

    public async Task<ChangeCompanyPlanResult> Handle(ChangeCompanyPlanCommand request, CancellationToken cancellationToken)
    {
        var subscription = await _session.Query<CompanySubscription>().GetOrThrowAsync(
            s => s.Company.Id == request.CompanyId,
            ErrorMessages.CompanySubscriptionNotFound(request.CompanyId),
            cancellationToken);

        if (subscription.AccessExpiresAt is null)
        {
            throw new ValidationException(ErrorMessages.CannotChangePlanWithoutPayment);
        }

        var effectiveDate = subscription.AccessExpiresAt.Value;

        var amountUsd = request.NewPlan switch
        {
            SubscriptionPlans.Basic => _options.BasicPlanAmountUsd,
            SubscriptionPlans.Premium => _options.PremiumPlanAmountUsd,
            _ => throw new ValidationException(ErrorMessages.InvalidSubscriptionPlan)
        };

        var rate = await _exchangeRateService.GetUsdToArsOficialRateAsync(cancellationToken);
        var amountArs = Math.Round(amountUsd * rate, 2, MidpointRounding.AwayFromZero);

        // end_date en una preapproval ya autorizada no lo respeta MercadoPago (confirmado en
        // pruebas: no tira error pero tampoco lo aplica). Cancelarla sí es confiable — y como
        // PendingPreapprovalId queda seteado, el webhook/sync no corta el acceso por esto,
        // solo por una cancelación real e independiente.
        var oldPreapprovalCancelled = await _mercadoPagoClient.CancelPreapprovalAsync(subscription.PreapprovalId, cancellationToken);

        var preapproval = await _mercadoPagoClient.CreatePreapprovalAsync(
            request.PayerEmail,
            request.CompanyId.ToString(),
            amountArs,
            cancellationToken,
            startDate: effectiveDate);

        using var transaction = _session.BeginTransaction();

        // El plan actual (y sus límites) no cambian todavía — solo queda "pendiente" hasta que
        // el cliente autorice esta preapproval nueva (confirmado por webhook o /sync).
        subscription.PendingPlan = request.NewPlan;
        subscription.PendingPlanAmountUsd = amountUsd;
        subscription.PendingUsdArsRate = rate;
        subscription.PendingAmountArs = amountArs;
        subscription.PendingPreapprovalId = preapproval.Id;
        subscription.Status = oldPreapprovalCancelled.Status;
        subscription.UpdatedAt = DateTime.UtcNow;
        await _session.UpdateAsync(subscription, cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return new ChangeCompanyPlanResult(preapproval.InitPoint, effectiveDate, oldPreapprovalCancelled.Status);
    }
}
