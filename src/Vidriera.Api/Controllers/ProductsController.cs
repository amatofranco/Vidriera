using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vidriera.Api.Common;
using Vidriera.Application.Common;
using Vidriera.Application.Products;

namespace Vidriera.Api.Controllers;

[ApiController]
[Route("api/products")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProductsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProductDto>>> GetProducts(CancellationToken cancellationToken)
    {
        var companyId = User.GetCompanyId();
        var result = await _mediator.Send(new GetProductsQuery(companyId), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [RequestSizeLimit(20_000_000)]
    public async Task<ActionResult<ProductDto>> CreateProduct([FromForm] CreateProductRequest request, CancellationToken cancellationToken)
    {
        var companyId = User.GetCompanyId();

        await using var stream = request.File.OpenReadStream();
        var result = await _mediator.Send(
            new CreateProductCommand(companyId, stream, request.File.FileName, request.Name, request.Code, request.Price),
            cancellationToken);

        return CreatedAtAction(nameof(GetProducts), null, result);
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

    [HttpPost("{id:guid}/sheet")]
    [RequestSizeLimit(20_000_000)]
    public async Task<IActionResult> UploadSheet(Guid id, IFormFile file, CancellationToken cancellationToken)
    {
        var companyId = User.GetCompanyId();

        await using var stream = file.OpenReadStream();
        await _mediator.Send(new UpdateProductSheetCommand(companyId, id, stream, file.FileName), cancellationToken);

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
    public async Task<IActionResult> DeleteProduct(Guid id, CancellationToken cancellationToken)
    {
        var companyId = User.GetCompanyId();
        await _mediator.Send(new DeleteProductCommand(companyId, id), cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:guid}/section")]
    public async Task<IActionResult> AssignSection(Guid id, [FromBody] AssignProductSectionRequest request, CancellationToken cancellationToken)
    {
        var companyId = User.GetCompanyId();
        await _mediator.Send(new AssignProductSectionCommand(companyId, id, request.SectionId), cancellationToken);
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

public record CreateProductRequest(IFormFile File, string? Name, string? Code, decimal? Price);

public record AssignProductSectionRequest(Guid? SectionId);

public record ReorderTopLevelItem(string Type, Guid Id);

public record ReorderTopLevelRequest(IReadOnlyList<ReorderTopLevelItem> Items);
