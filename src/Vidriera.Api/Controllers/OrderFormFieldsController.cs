using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vidriera.Api.Common;
using Vidriera.Application.Orders;

namespace Vidriera.Api.Controllers;

[ApiController]
[Route("api/order-form-fields")]
[Authorize]
public class OrderFormFieldsController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrderFormFieldsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OrderFormFieldDto>>> GetFields(CancellationToken cancellationToken)
    {
        var companyId = User.GetCompanyId();
        var result = await _mediator.Send(new GetOrderFormFieldsQuery(companyId), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<OrderFormFieldDto>> CreateField(
        [FromBody] CreateOrderFormFieldRequest request,
        CancellationToken cancellationToken)
    {
        var companyId = User.GetCompanyId();
        var result = await _mediator.Send(
            new CreateOrderFormFieldCommand(companyId, request.Label, request.FieldType, request.IsRequired),
            cancellationToken);
        return Ok(result);
    }

    [HttpPut("{fieldId:guid}")]
    public async Task<IActionResult> UpdateField(
        Guid fieldId,
        [FromBody] UpdateOrderFormFieldRequest request,
        CancellationToken cancellationToken)
    {
        var companyId = User.GetCompanyId();
        await _mediator.Send(
            new UpdateOrderFormFieldCommand(companyId, fieldId, request.Label, request.FieldType, request.IsRequired),
            cancellationToken);
        return NoContent();
    }

    [HttpDelete("{fieldId:guid}")]
    public async Task<IActionResult> DeleteField(Guid fieldId, CancellationToken cancellationToken)
    {
        var companyId = User.GetCompanyId();
        await _mediator.Send(new DeleteOrderFormFieldCommand(companyId, fieldId), cancellationToken);
        return NoContent();
    }

    [HttpPut("reorder")]
    public async Task<IActionResult> Reorder([FromBody] ReorderOrderFormFieldsRequest request, CancellationToken cancellationToken)
    {
        var companyId = User.GetCompanyId();
        await _mediator.Send(new ReorderOrderFormFieldsCommand(companyId, request.OrderedFieldIds), cancellationToken);
        return NoContent();
    }
}

public record CreateOrderFormFieldRequest(string Label, string FieldType, bool IsRequired);

public record UpdateOrderFormFieldRequest(string Label, string FieldType, bool IsRequired);

public record ReorderOrderFormFieldsRequest(IReadOnlyList<Guid> OrderedFieldIds);
