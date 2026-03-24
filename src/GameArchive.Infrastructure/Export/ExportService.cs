using CsvHelper;
using GameArchive.Application.Common;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GameArchive.Infrastructure.Export;

public class ExportService(IAppDbContext db)
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<byte[]> ExportCsvAsync()
    {
        var items = await db.Items
            .Include(i => i.ChecklistEntries)
            .OrderBy(i => i.Name)
            .ToListAsync();

        await using var ms = new MemoryStream();
        await using var writer = new StreamWriter(ms);
        await using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

        csv.WriteRecords(items.Select(i => new
        {
            i.Id,
            i.Name,
            Type       = i.Type.ToString(),
            i.Platform,
            i.Region,
            i.Condition,
            i.PurchasePrice,
            i.EstimatedValue,
            PurchaseDate = i.PurchaseDate?.ToString("yyyy-MM-dd"),
            Status     = i.Status.ToString(),
            i.Notes,
            ChecklistItems = string.Join(", ", i.ChecklistEntries.Select(e => $"{e.Label}:{(e.IsChecked ? "yes" : "no")}")),
            CreatedAt  = i.CreatedAt.ToString("yyyy-MM-dd")
        }));

        await writer.FlushAsync();
        return ms.ToArray();
    }

    public async Task<byte[]> ExportJsonAsync()
    {
        var items = await db.Items
            .Include(i => i.ChecklistEntries)
            .OrderBy(i => i.Name)
            .ToListAsync();

        var export = items.Select(i => new
        {
            i.Id,
            i.Name,
            Type           = i.Type.ToString(),
            i.Platform,
            i.Region,
            i.Condition,
            i.PurchasePrice,
            i.EstimatedValue,
            PurchaseDate   = i.PurchaseDate?.ToString("yyyy-MM-dd"),
            Status         = i.Status.ToString(),
            i.Notes,
            Checklist      = i.ChecklistEntries.Select(e => new { e.Label, e.IsChecked }),
            CreatedAt      = i.CreatedAt
        });

        return JsonSerializer.SerializeToUtf8Bytes(export, JsonOpts);
    }
}
