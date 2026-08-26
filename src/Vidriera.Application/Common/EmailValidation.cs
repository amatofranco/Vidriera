using System.Text.RegularExpressions;

namespace Vidriera.Application.Common;

public static class EmailValidation
{
    private static readonly Regex Pattern = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    public static bool IsValid(string value) => Pattern.IsMatch(value);
}
