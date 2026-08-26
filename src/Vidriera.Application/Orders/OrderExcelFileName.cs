namespace Vidriera.Application.Orders;

internal static class OrderExcelFileName
{
    public static string Build(DateTime timestamp) => $"{OrderLabels.DefaultFileNamePrefix}_{timestamp:yyyyMMdd_HHmm}.xlsx";
}
