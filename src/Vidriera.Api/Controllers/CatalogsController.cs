using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vidriera.Api.Common;
using Vidriera.Application.Catalogs;
using Vidriera.Application.Common;
using Vidriera.Application.Companies;
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
    public async Task<ActionResult<EnqueueCatalogGenerationResult>> Generate([FromQuery] bool showPrices, CancellationToken cancellationToken)
    {
        var companyId = User.GetCompanyId();
        var userId = User.GetUserId();

        var result = await _mediator.Send(new EnqueueCatalogGenerationCommand(companyId, userId, showPrices), cancellationToken);
        return Accepted(result);
    }

    [HttpGet("generation-jobs/active")]
    [Authorize]
    public async Task<ActionResult<CatalogGenerationJobStatusResult?>> GetActiveGenerationJob(CancellationToken cancellationToken)
    {
        var companyId = User.GetCompanyId();
        var result = await _mediator.Send(new GetActiveCatalogGenerationJobQuery(companyId), cancellationToken);
        return Ok(result);
    }

    [HttpGet("generation-jobs/{jobId:guid}")]
    [Authorize]
    public async Task<ActionResult<CatalogGenerationJobStatusResult>> GetGenerationJobStatus(Guid jobId, CancellationToken cancellationToken)
    {
        var companyId = User.GetCompanyId();
        var result = await _mediator.Send(new GetCatalogGenerationJobStatusQuery(jobId, companyId), cancellationToken);
        return Ok(result);
    }

    [HttpGet("current")]
    [Authorize]
    public async Task<ActionResult<GenerateCatalogResult?>> GetCurrent(CancellationToken cancellationToken)
    {
        var companyId = User.GetCompanyId();
        var result = await _mediator.Send(new GetCurrentCatalogQuery(companyId), cancellationToken);
        return Ok(result);
    }

    [HttpGet("company/{companyId:guid}")]
    [HttpGet("/{companyId:guid}")]
    [AllowAnonymous]
    public async Task<ContentResult> ViewByCompany(Guid companyId, CancellationToken cancellationToken)
    {
        try
        {
            var dto = await _mediator.Send(new GetCompanyCatalogQuery(companyId), cancellationToken);
            return HtmlPage(CatalogHtmlBuilder.BuildViewerPage(dto), StatusCodes.Status200OK);
        }
        catch (NotFoundException)
        {
            return HtmlPage(
                CatalogHtmlBuilder.BuildMessagePage("Catálogo no disponible", "Todavía no se generó un catálogo para esta empresa."),
                StatusCodes.Status404NotFound);
        }
    }

    [HttpGet("/")]
    [AllowAnonymous]
    public async Task<ContentResult> ViewByCustomDomain(CancellationToken cancellationToken)
    {
        var host = Request.Host.Host.ToLowerInvariant();
        if (host == "vidriera.app" || host == "catalogo.vidriera.app")
        {
            return HtmlPage(
                CatalogHtmlBuilder.BuildMessagePage("Catálogo no disponible", "Todavía no se generó un catálogo para esta empresa."),
                StatusCodes.Status404NotFound);
        }

        try
        {
            var dto = await _mediator.Send(new GetCompanyCatalogByCustomDomainQuery(host), cancellationToken);
            return HtmlPage(CatalogHtmlBuilder.BuildViewerPage(dto), StatusCodes.Status200OK);
        }
        catch (NotFoundException)
        {
            return HtmlPage(
                CatalogHtmlBuilder.BuildMessagePage("Catálogo no disponible", "Todavía no se generó un catálogo para esta empresa."),
                StatusCodes.Status404NotFound);
        }
    }

    [HttpGet("/{slug:regex(^[[^.]]+$)}")]
    [AllowAnonymous]
    public async Task<ContentResult> ViewBySlug(string slug, CancellationToken cancellationToken)
    {
        if (!CompanySlug.IsValid(slug))
        {
            return HtmlPage(
                CatalogHtmlBuilder.BuildMessagePage("Catálogo no disponible", "Todavía no se generó un catálogo para esta empresa."),
                StatusCodes.Status404NotFound);
        }

        try
        {
            var dto = await _mediator.Send(new GetCompanyCatalogBySlugQuery(slug), cancellationToken);
            return HtmlPage(CatalogHtmlBuilder.BuildViewerPage(dto), StatusCodes.Status200OK);
        }
        catch (NotFoundException)
        {
            return HtmlPage(
                CatalogHtmlBuilder.BuildMessagePage("Catálogo no disponible", "Todavía no se generó un catálogo para esta empresa."),
                StatusCodes.Status404NotFound);
        }
    }

    [HttpGet("company/{companyId:guid}/version")]
    [AllowAnonymous]
    public async Task<ActionResult<object>> GetCompanyCatalogVersion(Guid companyId, CancellationToken cancellationToken)
    {
        var catalogId = await _mediator.Send(new GetCompanyCatalogVersionQuery(companyId), cancellationToken);
        return Ok(new { catalogId });
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
    }

    [HttpGet("company/{companyId:guid}/cover-logo")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCoverLogo(Guid companyId, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(new GetCompanyCoverLogoQuery(companyId), cancellationToken);
            return File(result.Content, result.ContentType);
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet("company/{companyId:guid}/background")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCatalogBackground(Guid companyId, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(new GetCompanyBackgroundQuery(companyId), cancellationToken);
            return File(result.Content, result.ContentType);
        }
        catch (NotFoundException)
        {
            return NotFound();
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
    }

    private ContentResult HtmlPage(string html, int statusCode)
        => new() { Content = html, ContentType = "text/html; charset=utf-8", StatusCode = statusCode };
}
