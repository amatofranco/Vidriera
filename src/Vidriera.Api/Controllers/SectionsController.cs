using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vidriera.Api.Common;
using Vidriera.Application.Common;
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
            new CreateSectionCommand(companyId, stream, request.File.FileName, request.Name, request.ParentSectionId),
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

    [HttpPut("{id:guid}/parent")]
    public async Task<IActionResult> AssignParent(Guid id, [FromBody] AssignSectionParentRequest request, CancellationToken cancellationToken)
    {
        var companyId = User.GetCompanyId();
        await _mediator.Send(new AssignSectionParentCommand(companyId, id, request.ParentSectionId), cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:guid}/children/reorder")]
    public async Task<IActionResult> ReorderSectionChildren(
        Guid id,
        [FromBody] ReorderSectionChildrenRequest request,
        CancellationToken cancellationToken)
    {
        var companyId = User.GetCompanyId();
        var items = request.Items.Select(i => new OrderedItemRef(i.Type == "section", i.Id)).ToList();
        await _mediator.Send(
            new ReorderSectionChildrenCommand(companyId, id, items),
            cancellationToken);
        return NoContent();
    }
}

public record CreateSectionRequest(IFormFile File, string? Name, Guid? ParentSectionId);

public record AssignSectionParentRequest(Guid? ParentSectionId);

public record ReorderSectionChildrenItem(string Type, Guid Id);

public record ReorderSectionChildrenRequest(IReadOnlyList<ReorderSectionChildrenItem> Items);
