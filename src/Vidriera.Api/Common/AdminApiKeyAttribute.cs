using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Vidriera.Api.Common;

public class AdminApiKeyAttribute : Attribute, IAsyncActionFilter
{
    private const string HeaderName = "X-Admin-Api-Key";

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var expectedKey = configuration["Admin:ApiKey"];

        if (string.IsNullOrEmpty(expectedKey)
            || !context.HttpContext.Request.Headers.TryGetValue(HeaderName, out var providedKey)
            || providedKey != expectedKey)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        await next();
    }
}
