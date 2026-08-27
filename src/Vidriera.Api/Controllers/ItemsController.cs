using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vidriera.Api.Common;
using Vidriera.Application.Common;
using Vidriera.Application.Items;

namespace Vidriera.Api.Controllers;

[ApiController]
[Route("api/items")]
[Authorize]
public class ItemsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ItemsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ItemDto>>> GetItems(CancellationToken cancellationToken)
    {
        var companyId = User.GetCompanyId();
        var result = await _mediator.Send(new GetItemsQuery(companyId), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [RequestSizeLimit(60_000_000)]
    public async Task<ActionResult<ItemDto>> CreateItem([FromForm] CreateItemRequest request, CancellationToken cancellationToken)
    {
        var companyId = User.GetCompanyId();

        await using var stream = request.File.OpenReadStream();
        var result = await _mediator.Send(
            new CreateItemCommand(companyId, stream, request.File.FileName, request.Name, request.Code, request.Price),
            cancellationToken);

        return CreatedAtAction(nameof(GetItems), null, result);
    }

    [HttpPut("{id:guid}/stock")]
    public async Task<IActionResult> UpdateStock(Guid id, [FromBody] UpdateStockRequest request, CancellationToken cancellationToken)
    {
        var companyId = User.GetCompanyId();
        await _mediator.Send(new UpdateStockCommand(companyId, id, request.HasStock), cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:guid}/name")]
    public async Task<IActionResult> UpdateName(Guid id, [FromBody] UpdateNameRequest request, CancellationToken cancellationToken)
    {
        var companyId = User.GetCompanyId();
        await _mediator.Send(new UpdateNameCommand(companyId, id, request.Name), cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:guid}/price")]
    public async Task<IActionResult> UpdatePrice(Guid id, [FromBody] UpdatePriceRequest request, CancellationToken cancellationToken)
    {
        var companyId = User.GetCompanyId();
        await _mediator.Send(new UpdatePriceCommand(companyId, id, request.Price), cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:guid}/code")]
    public async Task<IActionResult> UpdateCode(Guid id, [FromBody] UpdateCodeRequest request, CancellationToken cancellationToken)
    {
        var companyId = User.GetCompanyId();
        await _mediator.Send(new UpdateCodeCommand(companyId, id, request.Code), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/sheet")]
    [RequestSizeLimit(60_000_000)]
    public async Task<IActionResult> UploadSheet(Guid id, IFormFile file, CancellationToken cancellationToken)
    {
        var companyId = User.GetCompanyId();

        await using var stream = file.OpenReadStream();
        await _mediator.Send(new UpdateItemSheetCommand(companyId, id, stream, file.FileName), cancellationToken);

        return NoContent();
    }

    [HttpPost("import-prices")]
    [RequestSizeLimit(20_000_000)]
    public async Task<ActionResult<ImportPricesResult>> ImportPrices(IFormFile file, CancellationToken cancellationToken)
    {
        var companyId = User.GetCompanyId();

        await using var stream = file.OpenReadStream();
        var result = await _mediator.Send(new ImportPricesCommand(companyId, stream), cancellationToken);

        return Ok(result);
    }

    [HttpGet("import-prices/template")]
    public async Task<IActionResult> DownloadPriceImportTemplate(CancellationToken cancellationToken)
    {
        var content = await _mediator.Send(new GetPriceImportTemplateQuery(), cancellationToken);
        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "plantilla-precios.xlsx");
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteItem(Guid id, CancellationToken cancellationToken)
    {
        var companyId = User.GetCompanyId();
        await _mediator.Send(new DeleteItemCommand(companyId, id), cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:guid}/section")]
    public async Task<IActionResult> AssignSection(Guid id, [FromBody] AssignItemSectionRequest request, CancellationToken cancellationToken)
    {
        var companyId = User.GetCompanyId();
        await _mediator.Send(new AssignItemSectionCommand(companyId, id, request.SectionId), cancellationToken);
        return NoContent();
    }

    [HttpPut("reorder")]
    public async Task<IActionResult> Reorder([FromBody] ReorderTopLevelRequest request, CancellationToken cancellationToken)
    {
        var companyId = User.GetCompanyId();
        var items = request.Items.Select(i => new OrderedItemRef(i.Type == "section", i.Id)).ToList();
        await _mediator.Send(new ReorderTopLevelCommand(companyId, items), cancellationToken);
        return NoContent();
    }
}

public record UpdateStockRequest(bool HasStock);

public record UpdateNameRequest(string Name);

public record UpdatePriceRequest(decimal? Price);

public record UpdateCodeRequest(string? Code);

public record CreateItemRequest(IFormFile File, string? Name, string? Code, decimal? Price);

public record AssignItemSectionRequest(Guid? SectionId);

public record ReorderTopLevelItem(string Type, Guid Id);

public record ReorderTopLevelRequest(IReadOnlyList<ReorderTopLevelItem> Items);
