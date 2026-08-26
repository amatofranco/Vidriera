using System.Text.RegularExpressions;

namespace Vidriera.Application.Common;

public static class CuitValidation
{
    private static readonly Regex Pattern = new(@"^\d{11}$", RegexOptions.Compiled);

    public static bool IsValid(string cuit) => Pattern.IsMatch(cuit);
}
