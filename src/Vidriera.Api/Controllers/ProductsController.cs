using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vidriera.Api.Common;
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
            new CreateProductCommand(companyId, stream, request.File.FileName, request.Name),
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

    [HttpPost("{id:guid}/sheet")]
    [RequestSizeLimit(20_000_000)]
    public async Task<IActionResult> UploadSheet(Guid id, IFormFile file, CancellationToken cancellationToken)
    {
        var companyId = User.GetCompanyId();

        await using var stream = file.OpenReadStream();
        await _mediator.Send(new UpdateProductSheetCommand(companyId, id, stream, file.FileName), cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteProduct(Guid id, CancellationToken cancellationToken)
    {
        var companyId = User.GetCompanyId();
        await _mediator.Send(new DeleteProductCommand(companyId, id), cancellationToken);
        return NoContent();
    }

    [HttpPut("reorder")]
    public async Task<IActionResult> Reorder([FromBody] ReorderProductsRequest request, CancellationToken cancellationToken)
    {
        var companyId = User.GetCompanyId();
        await _mediator.Send(new ReorderProductsCommand(companyId, request.ProductIds), cancellationToken);
        return NoContent();
    }
}

public record UpdateStockRequest(bool HasStock);

public record CreateProductRequest(IFormFile File, string? Name);

public record ReorderProductsRequest(IReadOnlyList<Guid> ProductIds);
