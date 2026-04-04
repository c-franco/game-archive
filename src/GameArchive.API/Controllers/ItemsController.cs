using GameArchive.Application.Features.Items.Commands;
using GameArchive.Application.Features.Items.Queries;
using GameArchive.Application.Resources;
using GameArchive.Domain;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GameArchive.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ItemsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public Task<List<Application.DTOs.CollectionItemDto>> Get(
        [FromQuery] string? search,
        [FromQuery] ItemType? type,
        [FromQuery] List<string>? platform,   // acepta platform=SNES&platform=N64
        [FromQuery] string? condition,
        [FromQuery] List<string>? region,     // acepta region=PAL&region=NTSC-U
        [FromQuery] ItemStatus? status,
        [FromQuery] string sortBy = "name",
        [FromQuery] bool desc = false)
        => mediator.Send(new GetItemsQuery(search, type, platform, condition, region, status, sortBy, desc));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var item = await mediator.Send(new GetItemByIdQuery(id));
        return item is null ? NotFound() : Ok(item);
    }

    [HttpGet("{id:guid}/edit-context")]
    public async Task<IActionResult> GetEditContext(Guid id)
    {
        var context = await mediator.Send(new GetItemEditContextQuery(id));
        return context is null ? NotFound() : Ok(context);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateItemCommand cmd)
    {
        var id = await mediator.Send(cmd);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateItemCommand cmd)
    {
        if (id != cmd.Id) return BadRequest(ServerStrings.Items.IdMismatch);
        try
        {
            await mediator.Send(cmd);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await mediator.Send(new DeleteItemCommand(id));
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("{id:guid}/toggle-status")]
    public async Task<IActionResult> ToggleStatus(Guid id)
    {
        try
        {
            var newStatus = await mediator.Send(new ToggleStatusCommand(id));
            return Ok(new { status = newStatus });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("{id:guid}/mark-owned")]
    public async Task<IActionResult> MarkOwned(Guid id)
    {
        try
        {
            var newStatus = await mediator.Send(new MarkAsOwnedCommand(id));
            return Ok(new { status = newStatus });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet("platforms")]
    public async Task<IActionResult> GetPlatforms()
    {
        var items = await mediator.Send(new GetItemsQuery(null, null, null, null, null, null));
        var platforms = items
            .Select(i => i.Platform)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Distinct()
            .OrderBy(p => p)
            .ToList();
        return Ok(platforms);
    }
}
