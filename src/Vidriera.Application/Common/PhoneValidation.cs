using System.Text.RegularExpressions;

namespace Vidriera.Application.Common;

public static class PhoneValidation
{
    private static readonly Regex Pattern = new(@"^\d+$", RegexOptions.Compiled);

    public static bool IsValid(string value) => Pattern.IsMatch(value);
}
