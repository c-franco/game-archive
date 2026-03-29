using GameArchive.Infrastructure.Import;
using Microsoft.AspNetCore.Mvc;

namespace GameArchive.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ImportController(ImportService import) : ControllerBase
{
    [HttpPost("csv")]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB
    public async Task<IActionResult> ImportCsv(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "No se ha enviado ningún fichero." });

        await using var stream = file.OpenReadStream();
        var result = await import.ImportCsvAsync(stream);
        return Ok(result);
    }

    [HttpPost("json")]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB
    public async Task<IActionResult> ImportJson(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "No se ha enviado ningún fichero." });

        await using var stream = file.OpenReadStream();
        var result = await import.ImportJsonAsync(stream);
        return Ok(result);
    }
}
