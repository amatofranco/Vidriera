using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Vidriera.Api.Common;

public class WorkerApiKeyAttribute : Attribute, IAsyncActionFilter
{
    private const string HeaderName = "X-Worker-Api-Key";

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var expectedKey = configuration["CatalogWorker:ApiKey"];

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
