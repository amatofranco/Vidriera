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

        // Un día antes, no el mismo día: evita cualquier ambigüedad sobre si el límite de MP
        // es inclusive y termina cobrando la vieja el mismo día que arranca la nueva.
        var oldPreapprovalUpdated = await _mercadoPagoClient.ScheduleEndDateAsync(
            subscription.PreapprovalId, effectiveDate.AddDays(-1), cancellationToken);

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
        subscription.UpdatedAt = DateTime.UtcNow;
        await _session.UpdateAsync(subscription, cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return new ChangeCompanyPlanResult(preapproval.InitPoint, effectiveDate, oldPreapprovalUpdated.EndDate);
    }
}
