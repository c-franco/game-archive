using CsvHelper;
using CsvHelper.Configuration;
using GameArchive.Application.Common;
using GameArchive.Application.Resources;
using GameArchive.Domain;
using GameArchive.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GameArchive.Infrastructure.Import;

public record ImportResult(int Created, int Updated, List<string> Errors);

public class ImportService(IAppDbContext db)
{
    public async Task<ImportResult> ImportJsonAsync(Stream stream)
    {
        JsonImportPayload payload;
        try
        {
            payload = await ReadJsonPayloadAsync(stream);
        }
        catch (Exception ex)
        {
            return new ImportResult(0, 0,
                [$"{ServerStrings.Import.ErrInvalidJson}{ex.Message}"]);
        }

        await ImportSettingsAsync(payload.Settings);
        return await UpsertRowsAsync(payload.Items.Select(MapJsonRow).ToList());
    }

    public async Task<ImportResult> ImportCsvAsync(Stream stream)
    {
        List<CsvImportRow> rawRows;
        try
        {
            using var reader = new StreamReader(stream);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HeaderValidated = null,
                MissingFieldFound = null
            });
            rawRows = csv.GetRecords<CsvImportRow>().ToList();
        }
        catch (Exception ex)
        {
            return new ImportResult(0, 0,
                [$"{ServerStrings.Import.ErrInvalidCsv}{ex.Message}"]);
        }

        await ImportSettingsAsync(new ImportSettingsPayload
        {
            Platforms = rawRows
                .Where(r => NormalizeRecordType(r.RecordType) == "platform")
                .Select(r => new ImportPlatformRow
                {
                    Name = r.Name,
                    SortOrder = r.SortOrder ?? 0
                })
                .ToList(),
            Regions = rawRows
                .Where(r => NormalizeRecordType(r.RecordType) == "region")
                .Select(r => new ImportRegionRow
                {
                    Name = r.Name,
                    SortOrder = r.SortOrder ?? 0
                })
                .ToList(),
            ChecklistTemplates = rawRows
                .Where(r => NormalizeRecordType(r.RecordType) == "checklist-template")
                .Select(r => new ImportChecklistTemplateRow
                {
                    ItemType = r.ItemType,
                    Label = r.Label,
                    SortOrder = r.SortOrder ?? 0
                })
                .ToList()
        });

        var rows = rawRows
            .Where(r =>
            {
                var recordType = NormalizeRecordType(r.RecordType);
                return recordType is "" or "item";
            })
            .Select(MapCsvRow)
            .ToList();

        return await UpsertRowsAsync(rows);
    }

    private async Task<ImportResult> UpsertRowsAsync(List<ImportRow> rows)
    {
        var templates = await db.ChecklistTemplates.ToListAsync();
        var errors = new List<string>();
        var created = 0;
        var updated = 0;

        foreach (var (row, index) in rows.Select((r, i) => (r, i + 1)))
        {
            if (string.IsNullOrWhiteSpace(row.Name))
            {
                errors.Add(ServerStrings.Import.ErrRowNameMissing(index));
                continue;
            }

            var itemType = ParseItemType(row.Type);
            if (itemType is null)
            {
                errors.Add(ServerStrings.Import.ErrUnknownType(index, row.Name, row.Type));
                continue;
            }

            var itemStatus = ParseItemStatus(row.Status);
            var checkedLabels = row.Checklist
                .Where(c => c.IsChecked)
                .Select(c => c.Label.Trim().ToLowerInvariant())
                .ToHashSet();

            var entries = itemStatus == ItemStatus.Wishlist
                ? []
                : templates
                    .Where(t => t.ItemType == itemType)
                    .OrderBy(t => t.SortOrder)
                    .Select(t => new ChecklistEntry
                    {
                        Label = t.Label,
                        IsChecked = checkedLabels.Contains(t.Label.Trim().ToLowerInvariant()),
                        SortOrder = t.SortOrder
                    })
                    .ToList();

            if (itemStatus == ItemStatus.Owned)
            {
                foreach (var extra in row.Checklist.Where(c =>
                    c.IsChecked &&
                    !entries.Any(e => e.Label.Trim().ToLowerInvariant() == c.Label.Trim().ToLowerInvariant())))
                {
                    entries.Add(new ChecklistEntry
                    {
                        Label = extra.Label,
                        IsChecked = true,
                        SortOrder = entries.Count + 1
                    });
                }
            }

            CollectionItem? existing = row.Id != Guid.Empty
                ? await db.Items.Include(i => i.ChecklistEntries)
                                .FirstOrDefaultAsync(i => i.Id == row.Id)
                : null;

            var purchasePrice = itemStatus == ItemStatus.Wishlist ? null : row.PurchasePrice;
            DateOnly? purchaseDate = itemStatus == ItemStatus.Wishlist
                ? null
                : row.PurchaseDate.HasValue ? DateOnly.FromDateTime(row.PurchaseDate.Value) : null;

            if (existing is not null)
            {
                existing.Name = row.Name;
                existing.Type = itemType.Value;
                existing.Platform = row.Platform;
                existing.Region = row.Region;
                existing.Condition = row.Condition;
                existing.PurchasePrice = purchasePrice;
                existing.EstimatedValue = row.EstimatedValue;
                existing.PurchaseDate = purchaseDate;
                existing.Notes = row.Notes;
                existing.Status = itemStatus;
                existing.UpdatedAt = DateTimeOffset.UtcNow;

                existing.ChecklistEntries.Clear();
                foreach (var entry in entries)
                    existing.ChecklistEntries.Add(entry);

                updated++;
            }
            else
            {
                db.Items.Add(new CollectionItem
                {
                    Id = row.Id != Guid.Empty ? row.Id : Guid.NewGuid(),
                    Name = row.Name,
                    Type = itemType.Value,
                    Platform = row.Platform,
                    Region = row.Region,
                    Condition = row.Condition,
                    PurchasePrice = purchasePrice,
                    EstimatedValue = row.EstimatedValue,
                    PurchaseDate = purchaseDate,
                    Notes = row.Notes,
                    Status = itemStatus,
                    ChecklistEntries = entries
                });
                created++;
            }
        }

        if (created > 0 || updated > 0)
            await db.SaveChangesAsync();

        return new ImportResult(created, updated, errors);
    }

    private async Task<JsonImportPayload> ReadJsonPayloadAsync(Stream stream)
    {
        using var document = await JsonDocument.ParseAsync(stream);
        var root = document.RootElement;

        if (root.ValueKind == JsonValueKind.Array)
        {
            return new JsonImportPayload
            {
                Items = JsonSerializer.Deserialize<List<JsonImportRow>>(root.GetRawText(), JsonOpts) ?? []
            };
        }

        if (root.ValueKind != JsonValueKind.Object)
            throw new JsonException("Unsupported JSON payload.");

        return JsonSerializer.Deserialize<JsonImportPayload>(root.GetRawText(), JsonOpts)
               ?? new JsonImportPayload();
    }

    private async Task ImportSettingsAsync(ImportSettingsPayload? settings)
    {
        if (settings is null)
            return;

        var existingPlatforms = await db.Platforms.ToListAsync();
        var existingRegions = await db.Regions.ToListAsync();
        var existingTemplates = await db.ChecklistTemplates.ToListAsync();
        var changed = false;

        foreach (var platform in settings.Platforms ?? [])
        {
            var name = platform.Name?.Trim();
            if (string.IsNullOrWhiteSpace(name))
                continue;

            var exists = existingPlatforms.Any(p =>
                string.Equals(p.Name.Trim(), name, StringComparison.OrdinalIgnoreCase));
            if (exists)
                continue;

            var sortOrder = platform.SortOrder > 0
                ? platform.SortOrder
                : existingPlatforms.Select(p => p.SortOrder).DefaultIfEmpty(0).Max() + 1;

            var entity = new Platform
            {
                Name = name,
                SortOrder = sortOrder
            };

            db.Platforms.Add(entity);
            existingPlatforms.Add(entity);
            changed = true;
        }

        foreach (var region in settings.Regions ?? [])
        {
            var name = region.Name?.Trim();
            if (string.IsNullOrWhiteSpace(name))
                continue;

            var exists = existingRegions.Any(r =>
                string.Equals(r.Name.Trim(), name, StringComparison.OrdinalIgnoreCase));
            if (exists)
                continue;

            var sortOrder = region.SortOrder > 0
                ? region.SortOrder
                : existingRegions.Select(r => r.SortOrder).DefaultIfEmpty(0).Max() + 1;

            var entity = new Region
            {
                Name = name,
                SortOrder = sortOrder
            };

            db.Regions.Add(entity);
            existingRegions.Add(entity);
            changed = true;
        }

        foreach (var template in settings.ChecklistTemplates ?? [])
        {
            var label = template.Label?.Trim();
            var itemType = ParseItemType(template.ItemType);
            if (string.IsNullOrWhiteSpace(label) || itemType is null)
                continue;

            var exists = existingTemplates.Any(t =>
                t.ItemType == itemType.Value &&
                string.Equals(t.Label.Trim(), label, StringComparison.OrdinalIgnoreCase));
            if (exists)
                continue;

            var sortOrder = template.SortOrder > 0
                ? template.SortOrder
                : existingTemplates.Where(t => t.ItemType == itemType.Value)
                    .Select(t => t.SortOrder)
                    .DefaultIfEmpty(0)
                    .Max() + 1;

            var entity = new ChecklistTemplate
            {
                ItemType = itemType.Value,
                Label = label,
                SortOrder = sortOrder
            };

            db.ChecklistTemplates.Add(entity);
            existingTemplates.Add(entity);
            changed = true;
        }

        if (changed)
            await db.SaveChangesAsync();
    }

    private static ImportRow MapJsonRow(JsonImportRow row) => new()
    {
        Id = row.Id,
        Name = row.Name ?? "",
        Type = row.Type ?? "",
        Platform = row.Platform ?? "",
        Region = row.Region ?? "",
        Condition = row.Condition ?? "",
        PurchasePrice = row.PurchasePrice,
        EstimatedValue = row.EstimatedValue,
        PurchaseDate = row.PurchaseDate,
        Notes = row.Notes ?? "",
        Status = row.Status ?? "Owned",
        Checklist = row.Checklist ?? []
    };

    private static ImportRow MapCsvRow(CsvImportRow row) => new()
    {
        Id = row.Id,
        Name = row.Name ?? "",
        Type = row.Type ?? "",
        Platform = row.Platform ?? "",
        Region = row.Region ?? "",
        Condition = row.Condition ?? "",
        PurchasePrice = TryDecimal(row.PurchasePrice),
        EstimatedValue = TryDecimal(row.EstimatedValue),
        PurchaseDate = TryDate(row.PurchaseDate),
        Notes = row.Notes ?? "",
        Status = row.Status ?? "Owned",
        Checklist = ParseCsvChecklist(row.ChecklistItems)
    };

    private static ItemType? ParseItemType(string? raw) => raw?.Trim().ToLowerInvariant() switch
    {
        "game" => ItemType.Game,
        "console" => ItemType.Console,
        _ => null
    };

    private static ItemStatus ParseItemStatus(string? raw) => raw?.Trim().ToLowerInvariant() switch
    {
        "wishlist" => ItemStatus.Wishlist,
        _ => ItemStatus.Owned
    };

    private static decimal? TryDecimal(string? s)
        => decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : null;

    private static DateTime? TryDate(string? s)
        => DateTime.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture,
            DateTimeStyles.None, out var d) ? d : null;

    private static List<ChecklistImportEntry> ParseCsvChecklist(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return [];

        return raw.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(part =>
            {
                var colon = part.LastIndexOf(':');
                if (colon < 0)
                    return null;

                var label = part[..colon].Trim();
                var isChecked = part[(colon + 1)..].Trim().ToLowerInvariant() == "yes";
                return string.IsNullOrEmpty(label) ? null : new ChecklistImportEntry(label, isChecked);
            })
            .Where(x => x is not null)
            .Select(x => x!)
            .ToList();
    }

    private static string NormalizeRecordType(string? recordType)
        => recordType?.Trim().ToLowerInvariant() ?? "";

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private class ImportRow
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
        public string Platform { get; set; } = "";
        public string Region { get; set; } = "";
        public string Condition { get; set; } = "";
        public decimal? PurchasePrice { get; set; }
        public decimal? EstimatedValue { get; set; }
        public DateTime? PurchaseDate { get; set; }
        public string Notes { get; set; } = "";
        public string Status { get; set; } = "Owned";
        public List<ChecklistImportEntry> Checklist { get; set; } = [];
    }

    private class JsonImportPayload
    {
        public List<JsonImportRow> Items { get; set; } = [];
        public ImportSettingsPayload? Settings { get; set; }
    }

    private class JsonImportRow
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public string? Type { get; set; }
        public string? Platform { get; set; }
        public string? Region { get; set; }
        public string? Condition { get; set; }
        public decimal? PurchasePrice { get; set; }
        public decimal? EstimatedValue { get; set; }
        [JsonConverter(typeof(FlexibleDateConverter))]
        public DateTime? PurchaseDate { get; set; }
        public string? Notes { get; set; }
        public string? Status { get; set; }
        public List<ChecklistImportEntry>? Checklist { get; set; }
    }

    private class ImportSettingsPayload
    {
        public List<ImportPlatformRow>? Platforms { get; set; }
        public List<ImportRegionRow>? Regions { get; set; }
        public List<ImportChecklistTemplateRow>? ChecklistTemplates { get; set; }
    }

    private class ImportPlatformRow
    {
        public string? Name { get; set; }
        public int SortOrder { get; set; }
    }

    private class ImportChecklistTemplateRow
    {
        public string? ItemType { get; set; }
        public string? Label { get; set; }
        public int SortOrder { get; set; }
    }

    private class ImportRegionRow
    {
        public string? Name { get; set; }
        public int SortOrder { get; set; }
    }

    private class CsvImportRow
    {
        public string? RecordType { get; set; }
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public string? Type { get; set; }
        public string? Platform { get; set; }
        public string? Region { get; set; }
        public string? Condition { get; set; }
        public string? PurchasePrice { get; set; }
        public string? EstimatedValue { get; set; }
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

public record ChecklistImportEntry(string Label, bool IsChecked);

public class FlexibleDateConverter : JsonConverter<DateTime?>
{
    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null) return null;
        var s = reader.GetString();
        if (string.IsNullOrWhiteSpace(s)) return null;
        if (DateTime.TryParse(s, CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind, out var dt)) return dt;
        return null;
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value,
        JsonSerializerOptions options)
    {
        if (value is null) writer.WriteNullValue();
        else writer.WriteStringValue(value.Value.ToString("yyyy-MM-dd"));
    }
}
