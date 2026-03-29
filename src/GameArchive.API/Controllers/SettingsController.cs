using GameArchive.Application.Features.Settings;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GameArchive.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SettingsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public Task<SettingsDto> Get()
        => mediator.Send(new GetSettingsQuery());

    [HttpPut]
    public async Task<IActionResult> Save([FromBody] SaveSettingsCommand cmd)
    {
        await mediator.Send(cmd);
        return NoContent();
    }
}
