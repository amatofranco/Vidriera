using System.Text.RegularExpressions;

namespace Vidriera.Application.Common;

public static class CompanySlug
{
    private static readonly Regex ValidPattern = new(@"^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.Compiled);

    public static string? Normalize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        return raw.Trim().ToLowerInvariant();
    }

    public static bool IsValid(string slug) => slug.Length <= 100 && ValidPattern.IsMatch(slug);
}
