using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vidriera.Application.Subscriptions;

namespace Vidriera.Api.Controllers;

[ApiController]
[Route("webhooks")]
[AllowAnonymous]
public class WebhooksController : ControllerBase
{
    private readonly IMediator _mediator;

    public WebhooksController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("mercadopago")]
    public async Task<IActionResult> MercadoPago(
        [FromQuery] string? type,
        [FromQuery] string? topic,
        [FromQuery] string? id,
        [FromQuery(Name = "data.id")] string? dataId,
        CancellationToken cancellationToken)
    {
        var eventType = type ?? topic;
        var resourceId = dataId ?? id;

        if (!string.IsNullOrEmpty(eventType) && !string.IsNullOrEmpty(resourceId))
        {
            await _mediator.Send(new ProcessMercadoPagoWebhookCommand(eventType, resourceId), cancellationToken);
        }

        return Ok();
    }
}
