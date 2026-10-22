using MediatR;
using Microsoft.AspNetCore.Mvc;
using WitcherKM.Workflow.Api.Contracts;
using WitcherKM.Workflow.Application;

namespace WitcherKM.Workflow.Api.Controllers;

[ApiController]
[Route("api/chains/{chainId:guid}/steps")]
public sealed class StepsController(ISender sender) : ControllerBase
{
    [HttpGet("{stepId:guid}")]
    public Task<StepView> Get(Guid chainId, Guid stepId, CancellationToken ct) =>
        sender.Send(new GetStep(chainId, stepId), ct);

    [HttpPost]
    public async Task<ActionResult<StepView>> Create(Guid chainId, [FromBody] StepInput input, CancellationToken ct)
    {
        var step = await sender.Send(new CreateStep(chainId, input.Title, input.Description), ct);
        return CreatedAtAction(nameof(Get), new { chainId, stepId = step.Id }, step);
    }

    [HttpPut("{stepId:guid}")]
    public Task<StepView> Update(Guid chainId, Guid stepId, [FromBody] StepInput input, CancellationToken ct) =>
        sender.Send(new UpdateStep(chainId, stepId, input.Title, input.Description), ct);

    [HttpDelete("{stepId:guid}")]
    public async Task<IActionResult> Delete(Guid chainId, Guid stepId, CancellationToken ct)
    {
        await sender.Send(new RemoveStep(chainId, stepId), ct);
        return NoContent();
    }

    [HttpPut("{stepId:guid}/position")]
    public Task<StepView> Move(Guid chainId, Guid stepId, [FromBody] PositionInput input, CancellationToken ct) =>
        sender.Send(new MoveStep(chainId, stepId, input.Position), ct);
}
