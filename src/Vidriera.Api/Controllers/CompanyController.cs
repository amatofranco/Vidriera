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
}
