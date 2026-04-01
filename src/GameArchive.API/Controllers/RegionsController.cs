using GameArchive.Application.Features.Regions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GameArchive.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RegionsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public Task<List<RegionDto>> Get()
        => mediator.Send(new GetRegionsQuery());

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] RegionRequest req)
    {
        var dto = await mediator.Send(new CreateRegionCommand(req.Name, req.SortOrder));
        return Ok(dto);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] RegionRequest req)
    {
        try
        {
            await mediator.Send(new UpdateRegionCommand(id, req.Name, req.SortOrder));
            return NoContent();
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await mediator.Send(new DeleteRegionCommand(id));
            return NoContent();
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    public record RegionRequest(string Name, int SortOrder);
}
