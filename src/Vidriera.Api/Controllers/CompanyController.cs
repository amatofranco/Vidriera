using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vidriera.Api.Common;
using Vidriera.Application.Companies;

namespace Vidriera.Api.Controllers;

[ApiController]
[Route("api/company")]
[Authorize]
public class CompanyController : ControllerBase
{
    private readonly IMediator _mediator;

    public CompanyController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("logo")]
    [RequestSizeLimit(5_000_000)]
    public async Task<IActionResult> UpdateLogo(IFormFile file, CancellationToken cancellationToken)
    {
        var companyId = User.GetCompanyId();

        await using var stream = file.OpenReadStream();
        await _mediator.Send(new UpdateCompanyLogoCommand(companyId, stream, file.ContentType), cancellationToken);

        return NoContent();
    }

    [HttpGet("logo")]
    public async Task<IActionResult> GetLogo(CancellationToken cancellationToken)
    {
        var companyId = User.GetCompanyId();
        var result = await _mediator.Send(new GetCompanyLogoQuery(companyId), cancellationToken);
        return File(result.Content, result.ContentType);
    }

    [HttpPost("cover-logo")]
    [RequestSizeLimit(5_000_000)]
    public async Task<IActionResult> UpdateCoverLogo(IFormFile file, CancellationToken cancellationToken)
    {
        var companyId = User.GetCompanyId();

        await using var stream = file.OpenReadStream();
        await _mediator.Send(new UpdateCompanyCoverLogoCommand(companyId, stream, file.ContentType), cancellationToken);

        return NoContent();
    }

    [HttpGet("cover-logo")]
    public async Task<IActionResult> GetCoverLogo(CancellationToken cancellationToken)
    {
        var companyId = User.GetCompanyId();
        var result = await _mediator.Send(new GetCompanyCoverLogoQuery(companyId), cancellationToken);
        return File(result.Content, result.ContentType);
    }

    [HttpDelete("cover-logo")]
    public async Task<IActionResult> DeleteCoverLogo(CancellationToken cancellationToken)
    {
        var companyId = User.GetCompanyId();
        await _mediator.Send(new DeleteCompanyCoverLogoCommand(companyId), cancellationToken);
        return NoContent();
    }

    [HttpPost("catalog-background")]
    [RequestSizeLimit(8_000_000)]
    public async Task<IActionResult> UpdateCatalogBackground(IFormFile file, CancellationToken cancellationToken)
    {
        var companyId = User.GetCompanyId();

        await using var stream = file.OpenReadStream();
        await _mediator.Send(new UpdateCompanyBackgroundCommand(companyId, stream, file.ContentType), cancellationToken);

        return NoContent();
    }

    [HttpGet("catalog-background")]
    public async Task<IActionResult> GetCatalogBackground(CancellationToken cancellationToken)
    {
        var companyId = User.GetCompanyId();
        var result = await _mediator.Send(new GetCompanyBackgroundQuery(companyId), cancellationToken);
        return File(result.Content, result.ContentType);
    }

    [HttpDelete("catalog-background")]
    public async Task<IActionResult> DeleteCatalogBackground(CancellationToken cancellationToken)
    {
        var companyId = User.GetCompanyId();
        await _mediator.Send(new DeleteCompanyBackgroundCommand(companyId), cancellationToken);
        return NoContent();
    }

    [HttpGet("catalog-cover-settings")]
    public async Task<ActionResult<CatalogCoverSettingsDto>> GetCatalogCoverSettings(CancellationToken cancellationToken)
    {
        var companyId = User.GetCompanyId();
        var result = await _mediator.Send(new GetCatalogCoverSettingsQuery(companyId), cancellationToken);
        return Ok(result);
    }

    [HttpPut("catalog-subtitle")]
    public async Task<IActionResult> SetCatalogSubtitle([FromBody] SetCatalogSubtitleRequest request, CancellationToken cancellationToken)
    {
        var companyId = User.GetCompanyId();
        await _mediator.Send(new SetCatalogSubtitleCommand(companyId, request.Subtitle), cancellationToken);
        return NoContent();
    }

    [HttpPut("catalog-validity-date")]
    public async Task<IActionResult> SetCatalogValidityDate([FromBody] SetCatalogValidityDateRequest request, CancellationToken cancellationToken)
    {
        var companyId = User.GetCompanyId();
        await _mediator.Send(new SetCatalogValidityDateCommand(companyId, request.CustomDate, request.Show), cancellationToken);
        return NoContent();
    }
}

public record SetCatalogSubtitleRequest(string? Subtitle);

public record SetCatalogValidityDateRequest(DateTime? CustomDate, bool Show);
