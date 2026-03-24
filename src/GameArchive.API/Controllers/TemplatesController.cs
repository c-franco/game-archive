using GameArchive.Application.DTOs;
using GameArchive.Application.Features.Templates;
using GameArchive.Domain;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GameArchive.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TemplatesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public Task<List<ChecklistTemplateDto>> Get()
        => mediator.Send(new GetTemplatesQuery());

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TemplateRequest req)
    {
        var dto = await mediator.Send(new CreateTemplateCommand(req.ItemType, req.Label, req.SortOrder));
        return Ok(dto);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] TemplateRequest req)
    {
        try
        {
            await mediator.Send(new UpdateTemplateCommand(id, req.Label, req.SortOrder));
            return NoContent();
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await mediator.Send(new DeleteTemplateCommand(id));
            return NoContent();
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    public record TemplateRequest(ItemType ItemType, string Label, int SortOrder);
}
