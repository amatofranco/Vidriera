using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vidriera.Api.Common;
using Vidriera.Application.Orders;

namespace Vidriera.Api.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrdersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OrderDto>>> GetOrders(CancellationToken cancellationToken)
    {
        var companyId = User.GetCompanyId();
        var result = await _mediator.Send(new GetOrdersQuery(companyId), cancellationToken);
        return Ok(result);
    }

    [HttpPost("excel")]
    [AllowAnonymous]
    public async Task<IActionResult> GenerateExcel([FromBody] GenerateOrderExcelRequest request, CancellationToken cancellationToken)
    {
        var command = new GenerateOrderExcelCommand(
            request.CompanyId,
            request.Items.Select(i => new OrderLineItem(i.ProductId, i.Quantity)).ToList(),
            new CustomerOrderInfo(
                request.Customer.BusinessName,
                request.Customer.StoreName,
                request.Customer.Cuit,
                request.Customer.VatCondition,
                request.Customer.Phone,
                request.Customer.Email,
                request.Customer.City,
                request.Customer.Province,
                request.Customer.Carrier,
                request.Customer.DeliveryAddress));

        var result = await _mediator.Send(command, cancellationToken);

        return File(
            result.Content,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            result.FileName);
    }
}

public record GenerateOrderExcelItemRequest(Guid ProductId, int Quantity);

public record GenerateOrderExcelCustomerRequest(
    string BusinessName,
    string StoreName,
    string Cuit,
    string VatCondition,
    string Phone,
    string Email,
    string City,
    string Province,
    string? Carrier,
    string DeliveryAddress);

public record GenerateOrderExcelRequest(
    Guid CompanyId,
    IReadOnlyList<GenerateOrderExcelItemRequest> Items,
    GenerateOrderExcelCustomerRequest Customer);
