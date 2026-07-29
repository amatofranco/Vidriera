using System.Security.Claims;
using Vidriera.Application.Common.Exceptions;

namespace Vidriera.Api.Common;

public static class CurrentUserExtensions
{
    public static Guid GetCompanyId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue("companyId");
        if (value is null || !Guid.TryParse(value, out var companyId))
        {
            throw new UnauthorizedException("El token no tiene un companyId válido.");
        }

        return companyId;
    }

    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (value is null || !Guid.TryParse(value, out var userId))
        {
            throw new UnauthorizedException("El token no tiene un identificador de usuario válido.");
        }

        return userId;
    }
}
