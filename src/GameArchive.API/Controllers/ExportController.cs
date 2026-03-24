using GameArchive.Infrastructure.Export;
using Microsoft.AspNetCore.Mvc;

namespace GameArchive.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExportController(ExportService export) : ControllerBase
{
    [HttpGet("csv")]
    public async Task<FileResult> Csv()
    {
        var data = await export.ExportCsvAsync();
        return File(data, "text/csv", $"gamearchive-{DateTime.Today:yyyyMMdd}.csv");
    }

    [HttpGet("json")]
    public async Task<FileResult> Json()
    {
        var data = await export.ExportJsonAsync();
        return File(data, "application/json", $"gamearchive-{DateTime.Today:yyyyMMdd}.json");
    }
}
