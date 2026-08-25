using Vidriera.Domain.Entities;

namespace Vidriera.Application.Subscriptions;

public static class PlanLimits
{
    public static int? MaxProducts(string plan) => plan switch
    {
        SubscriptionPlans.Basic => 100,
        SubscriptionPlans.Premium => 250,
        _ => null
    };

    public static int? MaxUsers(string plan) => plan switch
    {
        SubscriptionPlans.Basic => 1,
        SubscriptionPlans.Premium => 3,
        _ => null
    };
}
