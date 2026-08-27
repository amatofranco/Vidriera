using System.Text.RegularExpressions;

namespace Vidriera.Application.Common;

public static class CompanyCustomDomain
{
    private static readonly Regex ValidPattern = new(
        @"^(?!-)[a-z0-9-]{1,63}(?<!-)(\.(?!-)[a-z0-9-]{1,63}(?<!-))+$",
        RegexOptions.Compiled);

    public static string? Normalize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var value = raw.Trim().ToLowerInvariant();
        value = Regex.Replace(value, @"^https?://", "");
        value = value.Split('/')[0];
        value = value.TrimEnd('.');
        return value;
    }

    public static bool IsValid(string domain) => domain.Length <= 255 && ValidPattern.IsMatch(domain);
}
