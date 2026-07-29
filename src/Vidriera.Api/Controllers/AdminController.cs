using MediatR;
using Microsoft.AspNetCore.Mvc;
using Vidriera.Api.Common;
using Vidriera.Application.Admin;

namespace Vidriera.Api.Controllers;

[ApiController]
[Route("api/admin")]
[AdminApiKey]
public class AdminController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("companies")]
    public async Task<ActionResult<CreateCompanyResult>> CreateCompany(CreateCompanyCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPost("companies/{companyId:guid}/users")]
    public async Task<ActionResult<CreateUserResult>> AddUser(
        Guid companyId,
        [FromBody] AddUserRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new CreateUserCommand(companyId, request.Email, request.Name, request.Password),
            cancellationToken);

        return Ok(result);
    }
}

public record AddUserRequest(string Email, string Name, string Password);
