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
    private readonly ILogger<WebhooksController> _logger;

    public WebhooksController(IMediator mediator, ILogger<WebhooksController> logger)
    {
        _mediator = mediator;
        _logger = logger;
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
            try
            {
                await _mediator.Send(new ProcessMercadoPagoWebhookCommand(eventType, resourceId), cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error procesando webhook de MercadoPago: type={Type} id={ResourceId}", eventType, resourceId);

                // Un status distinto de 2xx hace que MercadoPago reintente la notificación más
                // tarde. Es seguro reprocesar: los pagos ya aplicados se descartan por payment_id
                // (processed_mercadopago_payments) y la preapproval se relee siempre de MP.
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        return Ok();
    }
}
