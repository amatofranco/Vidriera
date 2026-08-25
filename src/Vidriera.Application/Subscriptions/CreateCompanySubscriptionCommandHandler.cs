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

        var rate = await _exchangeRateService.GetUsdToArsOficialRateAsync(cancellationToken);
        var amountArs = Math.Round(amountUsd * rate, 2, MidpointRounding.AwayFromZero);

        var preapproval = await _mercadoPagoClient.CreatePreapprovalAsync(
            request.PayerEmail,
            company.Id.ToString(),
            amountArs,
            cancellationToken);

        var subscription = await _session.Query<CompanySubscription>()
            .FirstOrDefaultAsync(s => s.Company.Id == request.CompanyId, cancellationToken);

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

        return new CreateCompanySubscriptionResult(preapproval.InitPoint);
    }
}
