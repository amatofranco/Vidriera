using Vidriera.Application.Common;
using Vidriera.Application.Common.Exceptions;

namespace Vidriera.Application.Subscriptions;

internal static class SubscriptionDuration
{
    public const int MaxMonths = 120;

    /// <summary>
    /// Fecha de fin para una preapproval de <paramref name="months"/> cobros mensuales, o null si
    /// es indefinida. Se resta un día para que el cobro número months+1 (que caería justo en
    /// now + months) quede afuera y MercadoPago haga exactamente <paramref name="months"/> cobros.
    /// </summary>
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
