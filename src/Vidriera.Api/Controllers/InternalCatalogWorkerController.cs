using MediatR;
using Microsoft.AspNetCore.Mvc;
using Vidriera.Api.Common;
using Vidriera.Application.Catalogs;

namespace Vidriera.Api.Controllers;

[ApiController]
[Route("api/internal/catalog-jobs")]
[WorkerApiKey]
public class InternalCatalogWorkerController : ControllerBase
{
    private readonly IMediator _mediator;

    public InternalCatalogWorkerController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("{jobId:guid}/process")]
    public async Task<ActionResult> Process(Guid jobId)
    {
        // Sin CancellationToken ligado al request: si quien llamó (Render) se desconecta
        // antes de que termine, el job de generación no se tiene que cortar a mitad de camino.
        var processed = await _mediator.Send(new ProcessCatalogGenerationJobCommand(jobId), CancellationToken.None);
        return Ok(new { processed });
    }
}
