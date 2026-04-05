using GameArchive.Application.Common;
using GameArchive.Domain;
using GameArchive.Domain.Entities;
using GameArchive.Infrastructure.Pricing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace GameArchive.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PriceController(
    PriceChartingService priceService,
    IAppDbContext db) : ControllerBase
{
    /// <summary>
    /// POST /api/price/fetch/{id}
    /// Fetches price for a single item from PriceCharting and updates the database.
    /// </summary>
    [HttpPost("fetch/{id:guid}")]
    public async Task<IActionResult> FetchSingle(Guid id, CancellationToken ct)
    {
        try
        {
            var item = await db.Items
                .Include(i => i.ChecklistEntries)
                .FirstOrDefaultAsync(i => i.Id == id, ct);

            if (item == null)
                return NotFound(new { error = "Item not found" });

            if (item.Status != ItemStatus.Owned)
                return BadRequest(new { error = "Can only fetch prices for owned items" });

            var result = await priceService.FetchPriceAsync(item, ct);

            if (result.Success && result.PriceEur.HasValue)
            {
                item.EstimatedValue = result.PriceEur;
                item.PriceLastFetchedAt = DateTimeOffset.UtcNow;
                item.PriceSource = "PriceCharting";
                item.ProductUrl = result.ProductUrl ?? item.ProductUrl;
                item.UpdatedAt = DateTimeOffset.UtcNow;
                
                await db.SaveChangesAsync(ct);

                return Ok(new
                {
                    success = true,
                    priceEur = result.PriceEur,
                    condition = result.Condition,
                    productUrl = result.ProductUrl
                });
            }
            else
            {
                return Ok(new
                {
                    success = false,
                    error = result.ErrorMessage ?? "No se pudo obtener el precio"
                });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = $"Error interno: {ex.Message}" });
        }
    }

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
