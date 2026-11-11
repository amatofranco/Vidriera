using MediatR;
using Microsoft.Extensions.Options;
using NHibernate;
using NHibernate.Linq;
using Vidriera.Application.Abstractions;
using Vidriera.Application.Common;
using Vidriera.Application.Common.Exceptions;
using Vidriera.Domain.Entities;

namespace Vidriera.Application.Subscriptions;

public class CreateCompanySubscriptionCommandHandler : IRequestHandler<CreateCompanySubscriptionCommand, CreateCompanySubscriptionResult>
{
    private readonly ISession _session;
    private readonly IMercadoPagoClient _mercadoPagoClient;
    private readonly IExchangeRateService _exchangeRateService;
    private readonly MercadoPagoOptions _options;

    public CreateCompanySubscriptionCommandHandler(
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

    public async Task<CreateCompanySubscriptionResult> Handle(CreateCompanySubscriptionCommand request, CancellationToken cancellationToken)
    {
        var company = await _session.GetOrThrowAsync<Company>(
            request.CompanyId,
            ErrorMessages.CompanyNotFound(request.CompanyId),
            cancellationToken);

        var amountUsd = request.Plan switch
        {
            SubscriptionPlans.Basic => _options.BasicPlanAmountUsd,
            SubscriptionPlans.Premium => _options.PremiumPlanAmountUsd,
            _ => throw new ValidationException(ErrorMessages.InvalidSubscriptionPlan)
        };

        // Se valida antes de llamar a MercadoPago para no crear una preapproval huérfana.
        var endDate = SubscriptionDuration.EndDateFrom(DateTime.UtcNow, request.DurationMonths);

        var subscription = await _session.Query<CompanySubscription>()
            .FirstOrDefaultAsync(s => s.Company.Id == request.CompanyId, cancellationToken);

        if (subscription is not null)
        {
            await CancelPreviousPreapprovalsAsync(subscription, cancellationToken);
        }

        var rate = await _exchangeRateService.GetUsdToArsOficialRateAsync(cancellationToken);
        var amountArs = Math.Round(amountUsd * rate, 2, MidpointRounding.AwayFromZero);

        var preapproval = await _mercadoPagoClient.CreatePreapprovalAsync(
            request.PayerEmail,
            company.Id.ToString(),
            amountArs,
            cancellationToken,
            endDate: endDate);

        var now = DateTime.UtcNow;
        var isNew = subscription is null;
        subscription ??= new CompanySubscription { Id = Guid.NewGuid(), Company = company, CreatedAt = now };

        subscription.Plan = request.Plan;
        subscription.PlanAmountUsd = amountUsd;
        subscription.UsdArsRate = rate;
        subscription.AmountArs = amountArs;
        subscription.PreapprovalId = preapproval.Id;
        subscription.Status = preapproval.Status;
        subscription.UpdatedAt = now;
        PreapprovalCanceller.ClearPendingPlan(subscription);

        using var transaction = _session.BeginTransaction();

        if (isNew)
        {
            await _session.SaveAsync(subscription, cancellationToken);
        }
        else
        {
            await _session.UpdateAsync(subscription, cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);

        return new CreateCompanySubscriptionResult(preapproval.InitPoint, endDate);
    }

    /// <summary>
    /// Antes de generar un link nuevo, se da de baja en MercadoPago todo lo que haya quedado de
    /// antes (links sin autorizar, cambio de plan a medio hacer) para que el cliente no pueda
    /// autorizar dos suscripciones y pagar doble. Si alguna está autorizada (cobrando), se
    /// rechaza: para eso están el cambio de plan y la cancelación explícita.
    /// Los estados se leen en vivo de MP, no de la base, porque la base depende del webhook.
    /// </summary>
    private async Task CancelPreviousPreapprovalsAsync(CompanySubscription subscription, CancellationToken cancellationToken)
    {
        var current = await _mercadoPagoClient.GetPreapprovalAsync(subscription.PreapprovalId, cancellationToken);
        var pending = subscription.PendingPreapprovalId is null
            ? null
            : await _mercadoPagoClient.GetPreapprovalAsync(subscription.PendingPreapprovalId, cancellationToken);

        if (PreapprovalCanceller.IsAuthorized(current) || (pending is not null && PreapprovalCanceller.IsAuthorized(pending)))
        {
            throw new ValidationException(ErrorMessages.SubscriptionAlreadyActive);
        }

        await PreapprovalCanceller.CancelIfNotFinalAsync(_mercadoPagoClient, current, cancellationToken);
        if (pending is not null)
        {
            await PreapprovalCanceller.CancelIfNotFinalAsync(_mercadoPagoClient, pending, cancellationToken);
        }
    }
}
