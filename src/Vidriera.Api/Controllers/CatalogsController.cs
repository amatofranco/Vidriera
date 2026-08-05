using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vidriera.Api.Common;
using Vidriera.Application.Catalogs;
using Vidriera.Application.Common.Exceptions;

namespace Vidriera.Api.Controllers;

[ApiController]
[Route("api/catalogs")]
public class CatalogsController : ControllerBase
{
    private readonly IMediator _mediator;

    public CatalogsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [Authorize]
    public async Task Generate(
        [FromBody] GenerateCatalogRequest request,
        CancellationToken cancellationToken)
    {
        var companyId = User.GetCompanyId();
        var userId = User.GetUserId();

        Response.ContentType = "application/x-ndjson";

        async Task WriteLineAsync(object payload)
        {
            await Response.WriteAsync(JsonSerializer.Serialize(payload) + "\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }

        try
        {
            var result = await _mediator.Send(
                new GenerateCatalogCommand(
                    companyId,
                    userId,
                    request.ProductIds,
                    progress => WriteLineAsync(new { type = "progress", stage = progress.Stage, current = progress.Current, total = progress.Total })),
                cancellationToken);

            await WriteLineAsync(new { type = "result", data = result });
        }
        catch (Exception ex)
        {
            var statusCode = ex switch
            {
                NotFoundException => StatusCodes.Status404NotFound,
                ValidationException => StatusCodes.Status400BadRequest,
                CatalogGoneException => StatusCodes.Status410Gone,
                _ => StatusCodes.Status500InternalServerError
            };
            await WriteLineAsync(new { type = "error", status = statusCode, message = ex.Message });
        }
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<CatalogHistoryItemDto>>> GetHistory(CancellationToken cancellationToken)
    {
        var companyId = User.GetCompanyId();
        var result = await _mediator.Send(new GetCatalogHistoryQuery(companyId), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ContentResult> View(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var dto = await _mediator.Send(new GetGeneratedCatalogQuery(id), cancellationToken);
            return HtmlPage(CatalogHtmlBuilder.BuildViewerPage(dto), StatusCodes.Status200OK);
        }
        catch (NotFoundException)
        {
            return HtmlPage(
                CatalogHtmlBuilder.BuildMessagePage("Catálogo no encontrado", "El link no corresponde a ningún catálogo."),
                StatusCodes.Status404NotFound);
        }
        catch (CatalogGoneException ex)
        {
            return HtmlPage(
                CatalogHtmlBuilder.BuildMessagePage("Catálogo no disponible", ex.Message),
                StatusCodes.Status410Gone);
        }
    }

    [HttpGet("{id:guid}/file")]
    [AllowAnonymous]
    public async Task<IActionResult> DownloadFile(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(new GetCatalogFileQuery(id), cancellationToken);
            return File(result.Content, result.ContentType);
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
        catch (CatalogGoneException)
        {
            return StatusCode(StatusCodes.Status410Gone);
        }
    }

    [HttpGet("{id:guid}/pages/{pageNumber:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPage(Guid id, int pageNumber, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(new GetCatalogPageQuery(id, pageNumber), cancellationToken);
            return File(result.Content, result.ContentType);
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
        catch (CatalogGoneException)
        {
            return StatusCode(StatusCodes.Status410Gone);
        }
    }

    private ContentResult HtmlPage(string html, int statusCode)
        => new() { Content = html, ContentType = "text/html; charset=utf-8", StatusCode = statusCode };
}

public record GenerateCatalogRequest(IReadOnlyList<Guid> ProductIds);
