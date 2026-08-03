using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vidriera.Api.Common;
using Vidriera.Application.Products;
using Vidriera.Application.Sections;

namespace Vidriera.Api.Controllers;

[ApiController]
[Route("api/sections")]
[Authorize]
public class SectionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public SectionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SectionDto>>> GetSections(CancellationToken cancellationToken)
    {
        var companyId = User.GetCompanyId();
        var result = await _mediator.Send(new GetSectionsQuery(companyId), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [RequestSizeLimit(20_000_000)]
    public async Task<ActionResult<SectionDto>> CreateSection([FromForm] CreateSectionRequest request, CancellationToken cancellationToken)
    {
        var companyId = User.GetCompanyId();

        await using var stream = request.File.OpenReadStream();
        var result = await _mediator.Send(
            new CreateSectionCommand(companyId, stream, request.File.FileName, request.Name),
            cancellationToken);

        return CreatedAtAction(nameof(GetSections), null, result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteSection(Guid id, CancellationToken cancellationToken)
    {
        var companyId = User.GetCompanyId();
        await _mediator.Send(new DeleteSectionCommand(companyId, id), cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:guid}/products/reorder")]
    public async Task<IActionResult> ReorderSectionProducts(
        Guid id,
        [FromBody] ReorderSectionProductsRequest request,
        CancellationToken cancellationToken)
    {
        var companyId = User.GetCompanyId();
        await _mediator.Send(
            new ReorderSectionProductsCommand(companyId, id, request.ProductIds),
            cancellationToken);
        return NoContent();
    }
}

public record CreateSectionRequest(IFormFile File, string? Name);

public record ReorderSectionProductsRequest(IReadOnlyList<Guid> ProductIds);
