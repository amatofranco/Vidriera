using Vidriera.Application.Common;
using Vidriera.Application.Common.Exceptions;

namespace Vidriera.Application.Subscriptions;

internal static class SubscriptionDuration
{
    public const int MaxMonths = 120;

    public static DateTime? EndDateFrom(DateTime now, int? months)
    {
        if (months is null)
        {
            return null;
        }

        if (months < 1 || months > MaxMonths)
        {
            throw new ValidationException(ErrorMessages.InvalidSubscriptionDuration(MaxMonths));
        }

        return now.AddMonths(months.Value).AddDays(-1);
    }
}
