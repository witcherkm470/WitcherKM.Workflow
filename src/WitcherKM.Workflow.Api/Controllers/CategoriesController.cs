using MediatR;
using Microsoft.AspNetCore.Mvc;
using WitcherKM.Workflow.Api.Contracts;
using WitcherKM.Workflow.Application;

namespace WitcherKM.Workflow.Api.Controllers;

[ApiController]
[Route("api/categories")]
public sealed class CategoriesController(ISender sender) : ControllerBase
{
    [HttpGet]
    public Task<List<CategoryView>> List([FromQuery] string? name, CancellationToken ct) =>
        sender.Send(new ListCategories(name), ct);

    [HttpGet("{id:guid}")]
    public Task<CategoryView> Get(Guid id, CancellationToken ct) =>
        sender.Send(new GetCategory(id), ct);

    [HttpPost]
    public async Task<ActionResult<CategoryView>> Create([FromBody] NameInput input, CancellationToken ct)
    {
        var category = await sender.Send(new CreateCategory(input.Name), ct);
        return CreatedAtAction(nameof(Get), new { id = category.Id }, category);
    }

    [HttpPut("{id:guid}")]
    public Task<CategoryView> Update(Guid id, [FromBody] NameInput input, CancellationToken ct) =>
        sender.Send(new UpdateCategory(id, input.Name), ct);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await sender.Send(new RemoveCategory(id), ct);
        return NoContent();
    }
}
