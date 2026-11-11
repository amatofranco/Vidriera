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

        // Si la suscripción actual tiene fecha de fin (ej. contrato anual), la nueva hereda esa
        // misma fecha: cambiar de plan no extiende el contrato. Se consulta antes de marcar el
        // cambio pendiente para poder rechazarlo sin dejar nada a medio hacer.
        var currentPreapproval = await _mercadoPagoClient.GetPreapprovalAsync(subscription.PreapprovalId, cancellationToken);

        // Si ya había un cambio de plan pedido antes, su link también hay que darlo de baja —
        // si no, el cliente podría autorizar los dos y pagar doble. Si ese link ya fue
        // autorizado, el cambio está hecho (falta que llegue el webhook o correr /sync).
        var previousPending = subscription.PendingPreapprovalId is null
            ? null
            : await _mercadoPagoClient.GetPreapprovalAsync(subscription.PendingPreapprovalId, cancellationToken);
        if (previousPending is not null && PreapprovalCanceller.IsAuthorized(previousPending))
        {
            throw new ValidationException(ErrorMessages.PlanChangeAlreadyAuthorized);
        }

        var endDate = currentPreapproval.EndDate?.UtcDateTime;
        if (endDate.HasValue && endDate.Value <= effectiveDate)
        {
            throw new ValidationException(ErrorMessages.SubscriptionEndsBeforePlanChange);
        }

        var rate = await _exchangeRateService.GetUsdToArsOficialRateAsync(cancellationToken);
        var amountArs = Math.Round(amountUsd * rate, 2, MidpointRounding.AwayFromZero);

        // PendingPlan se guarda ANTES de tocar MercadoPago — es la señal que usan el webhook y
        // el /sync para saber que una cancelación es parte de un cambio de plan en curso, no una
        // cancelación real. Si se guardara después de cancelar, el webhook de esa cancelación
        // podría llegar y procesarse en el medio, antes de que quede guardado, y cortar el acceso
        // por error (pasó en producción: race condition real, no hipotética).
        using (var markPendingTransaction = _session.BeginTransaction())
        {
            subscription.PendingPlan = request.NewPlan;
            subscription.PendingPlanAmountUsd = amountUsd;
            subscription.PendingUsdArsRate = rate;
            subscription.PendingAmountArs = amountArs;
            subscription.UpdatedAt = DateTime.UtcNow;
            await _session.UpdateAsync(subscription, cancellationToken);
            await markPendingTransaction.CommitAsync(cancellationToken);
        }

        // end_date en una preapproval ya autorizada no lo respeta MercadoPago (confirmado en
        // pruebas: no tira error pero tampoco lo aplica). Cancelarla sí es confiable.
        // CancelIfNotFinal hace que el cambio se pueda reintentar: si una corrida anterior canceló
        // la preapproval vieja y después falló al crear la nueva, la segunda corrida no vuelve a
        // cancelar (MP puede dar error con una ya cancelada) y sigue directo a crear la nueva.
        var oldPreapprovalCancelled = await PreapprovalCanceller.CancelIfNotFinalAsync(_mercadoPagoClient, currentPreapproval, cancellationToken);
        if (previousPending is not null)
        {
            await PreapprovalCanceller.CancelIfNotFinalAsync(_mercadoPagoClient, previousPending, cancellationToken);
        }

        var preapproval = await _mercadoPagoClient.CreatePreapprovalAsync(
            request.PayerEmail,
            request.CompanyId.ToString(),
            amountArs,
            cancellationToken,
            startDate: effectiveDate,
            endDate: endDate);

        using var transaction = _session.BeginTransaction();

        subscription.PendingPreapprovalId = preapproval.Id;
        subscription.Status = oldPreapprovalCancelled.Status;
        subscription.UpdatedAt = DateTime.UtcNow;
        await _session.UpdateAsync(subscription, cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return new ChangeCompanyPlanResult(preapproval.InitPoint, effectiveDate, oldPreapprovalCancelled.Status);
    }
}
