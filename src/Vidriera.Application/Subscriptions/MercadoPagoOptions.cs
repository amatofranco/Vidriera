namespace Vidriera.Application.Subscriptions;

public class MercadoPagoOptions
{
    public string AccessToken { get; set; } = null!;
    public decimal BasicPlanAmountUsd { get; set; } = 5m;
    public decimal PremiumPlanAmountUsd { get; set; } = 10m;
    public string CurrencyId { get; set; } = "ARS";
    public int GracePeriodDays { get; set; } = 5;
    public string NotificationUrl { get; set; } = null!;
    public string BackUrl { get; set; } = null!;
}
