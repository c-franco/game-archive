using GameArchive.Application.Features.Stats;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GameArchive.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StatsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public Task<StatsDto> Get() => mediator.Send(new GetStatsQuery());
}
