namespace Vidriera.Application.Orders;

internal static class OrderExcelFileName
{
    public static string Build(string businessName, DateTime timestamp)
    {
        var sanitized = Sanitize(businessName);
        return $"{OrderLabels.DefaultFileNamePrefix}_{sanitized}_{timestamp:yyyyMMdd_HHmm}.xlsx";
    }

    private static string Sanitize(string value)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(value.Where(c => !invalidChars.Contains(c)).ToArray()).Trim();
        return string.IsNullOrWhiteSpace(sanitized) ? OrderLabels.DefaultFileNamePrefix : sanitized.Replace(' ', '_');
    }
}
