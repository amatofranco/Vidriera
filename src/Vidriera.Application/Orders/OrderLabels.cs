using System.Globalization;
using System.Resources;

namespace Vidriera.Application.Orders;

public static class OrderLabels
{
    private static readonly ResourceManager Resources = new(
        "Vidriera.Application.Orders.OrderLabels",
        typeof(OrderLabels).Assembly);

    private static string Get(string name) => Resources.GetString(name, CultureInfo.CurrentUICulture)!;

    public static string DefaultFileNamePrefix => Get(nameof(DefaultFileNamePrefix));
}
