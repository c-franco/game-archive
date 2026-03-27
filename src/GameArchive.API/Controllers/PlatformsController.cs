using GameArchive.Application.Features.Platforms;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GameArchive.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PlatformsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public Task<List<PlatformDto>> Get()
        => mediator.Send(new GetPlatformsQuery());

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PlatformRequest req)
    {
        var dto = await mediator.Send(new CreatePlatformCommand(req.Name, req.SortOrder));
        return Ok(dto);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] PlatformRequest req)
    {
        try
        {
            await mediator.Send(new UpdatePlatformCommand(id, req.Name, req.SortOrder));
            return NoContent();
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await mediator.Send(new DeletePlatformCommand(id));
            return NoContent();
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    public record PlatformRequest(string Name, int SortOrder);
}
