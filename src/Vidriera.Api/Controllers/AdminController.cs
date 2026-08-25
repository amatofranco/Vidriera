using MediatR;
using Microsoft.AspNetCore.Mvc;
using Vidriera.Api.Common;
using Vidriera.Application.Admin;
using Vidriera.Application.Subscriptions;

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

    [HttpPost("companies/{companyId:guid}/subscription")]
    public async Task<ActionResult<CreateCompanySubscriptionResult>> CreateSubscription(
        Guid companyId,
        [FromBody] CreateSubscriptionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new CreateCompanySubscriptionCommand(companyId, request.PayerEmail, request.Plan),
            cancellationToken);

        return Ok(result);
    }

    [HttpPut("companies/{companyId:guid}/subscription/exempt")]
    public async Task<IActionResult> SetSubscriptionExempt(
        Guid companyId,
        [FromBody] SetSubscriptionExemptRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new SetCompanySubscriptionExemptCommand(companyId, request.IsExempt), cancellationToken);
        return NoContent();
    }

    [HttpPost("companies/{companyId:guid}/subscription/sync")]
    public async Task<ActionResult<SyncCompanySubscriptionResult>> SyncSubscription(
        Guid companyId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new SyncCompanySubscriptionCommand(companyId), cancellationToken);
        return Ok(result);
    }

    [HttpPost("companies/{companyId:guid}/subscription/cancel")]
    public async Task<IActionResult> CancelSubscription(Guid companyId, CancellationToken cancellationToken)
    {
        await _mediator.Send(new CancelCompanySubscriptionCommand(companyId), cancellationToken);
        return NoContent();
    }

    [HttpPost("companies/{companyId:guid}/subscription/change-plan")]
    public async Task<ActionResult<ChangeCompanyPlanResult>> ChangePlan(
        Guid companyId,
        [FromBody] ChangePlanRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new ChangeCompanyPlanCommand(companyId, request.PayerEmail, request.NewPlan),
            cancellationToken);

        return Ok(result);
    }
}

public record AddUserRequest(string Email, string Name, string Password);

public record CreateSubscriptionRequest(string PayerEmail, string Plan);

public record SetSubscriptionExemptRequest(bool IsExempt);

public record ChangePlanRequest(string PayerEmail, string NewPlan);
