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
        var platforms = await db.Platforms
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Name)
            .ToListAsync();
        var regions = await db.Regions
            .OrderBy(r => r.SortOrder)
            .ThenBy(r => r.Name)
            .ToListAsync();
        var templates = await db.ChecklistTemplates
            .OrderBy(t => t.ItemType)
            .ThenBy(t => t.SortOrder)
            .ThenBy(t => t.Label)
            .ToListAsync();

        await using var ms = new MemoryStream();
        await using var writer = new StreamWriter(ms);
        await using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

        var rows = items.Select(i => new ExportCsvRow
        {
            RecordType    = "item",
            Id            = i.Id,
            Name          = i.Name,
            Type          = i.Type.ToString(),
            Platform      = i.Platform,
            Region        = i.Region,
            Condition     = i.Condition,
            PurchasePrice = i.PurchasePrice,
            EstimatedValue = i.EstimatedValue,
            PurchaseDate  = i.PurchaseDate?.ToString("yyyy-MM-dd"),
            Status        = i.Status.ToString(),
            Notes         = i.Notes,
            ChecklistItems = string.Join(", ", i.ChecklistEntries.Select(e => $"{e.Label}:{(e.IsChecked ? "yes" : "no")}")),
            CreatedAt     = i.CreatedAt.ToString("yyyy-MM-dd")
        })
        .Concat(platforms.Select(p => new ExportCsvRow
        {
            RecordType = "platform",
            Id         = p.Id,
            Name       = p.Name,
            SortOrder  = p.SortOrder
        }))
        .Concat(regions.Select(r => new ExportCsvRow
        {
            RecordType = "region",
            Id         = r.Id,
            Name       = r.Name,
            SortOrder  = r.SortOrder
        }))
        .Concat(templates.Select(t => new ExportCsvRow
        {
            RecordType = "checklist-template",
            Id         = t.Id,
            ItemType   = t.ItemType.ToString(),
            Label      = t.Label,
            SortOrder  = t.SortOrder
        }))
        .ToList();

        csv.WriteRecords(rows);

        await writer.FlushAsync();
        return ms.ToArray();
    }

    public async Task<byte[]> ExportJsonAsync()
    {
        var items = await db.Items
            .Include(i => i.ChecklistEntries)
            .OrderBy(i => i.Name)
            .ToListAsync();
        var platforms = await db.Platforms
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Name)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.SortOrder
            })
            .ToListAsync();
        var regions = await db.Regions
            .OrderBy(r => r.SortOrder)
            .ThenBy(r => r.Name)
            .Select(r => new
            {
                r.Id,
                r.Name,
                r.SortOrder
            })
            .ToListAsync();
        var templates = await db.ChecklistTemplates
            .OrderBy(t => t.ItemType)
            .ThenBy(t => t.SortOrder)
            .ThenBy(t => t.Label)
            .Select(t => new
            {
                t.Id,
                ItemType = t.ItemType.ToString(),
                t.Label,
                t.SortOrder
            })
            .ToListAsync();

        var export = new
        {
            Items = items.Select(i => new
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
            }),
            Settings = new
            {
                Platforms = platforms,
                Regions = regions,
                ChecklistTemplates = templates
            }
        };

        return JsonSerializer.SerializeToUtf8Bytes(export, JsonOpts);
    }

    private sealed class ExportCsvRow
    {
        public string RecordType { get; set; } = "";
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public string? Type { get; set; }
        public string? Platform { get; set; }
        public string? Region { get; set; }
        public string? Condition { get; set; }
        public decimal? PurchasePrice { get; set; }
        public decimal? EstimatedValue { get; set; }
        public string? PurchaseDate { get; set; }
        public string? Status { get; set; }
        public string? Notes { get; set; }
        public string? ChecklistItems { get; set; }
        public string? CreatedAt { get; set; }
        public string? ItemType { get; set; }
        public string? Label { get; set; }
        public int? SortOrder { get; set; }
    }
}
