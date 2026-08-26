using System.Text.RegularExpressions;

namespace Vidriera.Application.Common;

public static class NameValidation
{
    private static readonly Regex Pattern = new(@"^[\p{L}\s'-]+$", RegexOptions.Compiled);

    public static bool IsValid(string value) => Pattern.IsMatch(value);
}
