using GameArchive.Infrastructure.Pricing;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace GameArchive.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PriceController(PriceChartingService priceService) : ControllerBase
{
    /// <summary>
    /// SSE endpoint. Streams { done, total, finished } events.
    /// Final event adds: updated, failed, errors[].
    /// POST /api/price/fetch-all
    /// </summary>
    [HttpPost("fetch-all")]
    public async Task FetchAll(CancellationToken ct)
    {
        Response.Headers.Append("Content-Type",     "text/event-stream");
        Response.Headers.Append("Cache-Control",    "no-cache");
        Response.Headers.Append("X-Accel-Buffering","no");

        async Task Send(object payload)
        {
            try
            {
                await Response.WriteAsync("data: " + JsonSerializer.Serialize(payload) + "\n\n", ct);
                await Response.Body.FlushAsync(ct);
            }
            catch { /* client disconnected */ }
        }

        var progress = new Progress<(int done, int total)>(p =>
            _ = Send(new { done = p.done, total = p.total, finished = false }));

        BatchPriceResult result;
        try
        {
            result = await priceService.FetchAllPricesAsync(progress, ct);
        }
        catch (OperationCanceledException) { return; }
        catch (Exception ex)
        {
            await Send(new { finished = true, updated = 0, failed = 0,
                             errors = new[] { ex.Message } });
            return;
        }

        await Send(new
        {
            finished = true,
            updated  = result.Updated,
            failed   = result.Failed,
            errors   = result.Errors
        });
    }
}
