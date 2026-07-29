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
    public async Task<ActionResult<GenerateCatalogResult>> Generate(
        [FromBody] GenerateCatalogRequest request,
        CancellationToken cancellationToken)
    {
        var companyId = User.GetCompanyId();
        var userId = User.GetUserId();

        var result = await _mediator.Send(
            new GenerateCatalogCommand(companyId, userId, request.ProductIds),
            cancellationToken);

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
            return File(result.Content, result.ContentType, result.FileName);
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
