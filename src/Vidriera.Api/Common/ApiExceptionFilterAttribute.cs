using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Vidriera.Application.Common.Exceptions;

namespace Vidriera.Api.Common;

public class ApiExceptionFilterAttribute : ExceptionFilterAttribute
{
    public override void OnException(ExceptionContext context)
    {
        var (statusCode, title) = context.Exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "No encontrado"),
            ValidationException => (StatusCodes.Status400BadRequest, "Solicitud inválida"),
            UnauthorizedException => (StatusCodes.Status401Unauthorized, "No autorizado"),
            CatalogGoneException => (StatusCodes.Status410Gone, "Recurso expirado o revocado"),
            _ => (StatusCodes.Status500InternalServerError, "Error interno")
        };

        context.Result = new ObjectResult(new ProblemDetails
        {
            Title = title,
            Status = statusCode,
            Detail = context.Exception.Message
        })
        {
            StatusCode = statusCode
        };

        context.ExceptionHandled = true;
    }
}
