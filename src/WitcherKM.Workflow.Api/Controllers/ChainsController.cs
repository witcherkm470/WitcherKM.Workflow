using MediatR;
using Microsoft.AspNetCore.Mvc;
using WitcherKM.Workflow.Api.Contracts;
using WitcherKM.Workflow.Application;

namespace WitcherKM.Workflow.Api.Controllers;

[ApiController]
[Route("api/chains")]
public sealed class ChainsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public Task<List<ChainListView>> List([FromQuery] string? name, [FromQuery] Guid? categoryId, CancellationToken ct) =>
        sender.Send(new ListChains(name, categoryId), ct);

    [HttpGet("{id:guid}")]
    public Task<ChainView> Get(Guid id, CancellationToken ct) =>
        sender.Send(new GetChain(id), ct);

    [HttpPost]
    public async Task<ActionResult<ChainView>> Create([FromBody] ChainInput input, CancellationToken ct)
    {
        var chain = await sender.Send(new CreateChain(input.Name, input.CategoryIds), ct);
        return CreatedAtAction(nameof(Get), new { id = chain.Id }, chain);
    }

    [HttpPut("{id:guid}")]
    public Task<ChainView> Update(Guid id, [FromBody] ChainInput input, CancellationToken ct) =>
        sender.Send(new UpdateChain(id, input.Name, input.CategoryIds), ct);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await sender.Send(new RemoveChain(id), ct);
        return NoContent();
    }
}
