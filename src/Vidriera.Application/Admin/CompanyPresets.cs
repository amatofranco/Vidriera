using Vidriera.Application.Common;
using Vidriera.Application.Common.Exceptions;

namespace Vidriera.Application.Admin;

public static class CompanyPresets
{
    public const string Magazine = "Magazine";

    public static (bool ShowCode, bool ShowPrice, bool ShowOrders) Resolve(string preset) => preset switch
    {
        Magazine => (false, false, false),
        _ => throw new ValidationException(ErrorMessages.InvalidCompanyPreset(preset)),
    };
}
