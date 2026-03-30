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
    // ── JSON ──────────────────────────────────────────────────────────────────

    public async Task<ImportResult> ImportJsonAsync(Stream stream)
    {
        List<JsonImportRow> rows;
        try
        {
            rows = await JsonSerializer.DeserializeAsync<List<JsonImportRow>>(stream, JsonOpts)
                   ?? [];
        }
        catch (Exception ex)
        {
            return new ImportResult(0, 0,
                [$"{ServerStrings.Import.ErrInvalidJson}{ex.Message}"]);
        }

        return await UpsertRowsAsync(rows.Select(r => new ImportRow
        {
            Id             = r.Id,
            Name           = r.Name ?? "",
            Type           = r.Type ?? "",
            Platform       = r.Platform ?? "",
            Region         = r.Region ?? "",
            Condition      = r.Condition ?? "",
            PurchasePrice  = r.PurchasePrice,
            EstimatedValue = r.EstimatedValue,
            PurchaseDate   = r.PurchaseDate,
            Notes          = r.Notes ?? "",
            Status         = r.Status ?? "Owned",
            Checklist      = r.Checklist ?? []
        }).ToList());
    }

    // ── CSV ───────────────────────────────────────────────────────────────────

    public async Task<ImportResult> ImportCsvAsync(Stream stream)
    {
        List<CsvImportRow> rawRows;
        try
        {
            using var reader = new StreamReader(stream);
            using var csv    = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HeaderValidated   = null,
                MissingFieldFound = null
            });
            rawRows = csv.GetRecords<CsvImportRow>().ToList();
        }
        catch (Exception ex)
        {
            return new ImportResult(0, 0,
                [$"{ServerStrings.Import.ErrInvalidCsv}{ex.Message}"]);
        }

        var rows = rawRows.Select(r => new ImportRow
        {
            Id             = r.Id,
            Name           = r.Name ?? "",
            Type           = r.Type ?? "",
            Platform       = r.Platform ?? "",
            Region         = r.Region ?? "",
            Condition      = r.Condition ?? "",
            PurchasePrice  = TryDecimal(r.PurchasePrice),
            EstimatedValue = TryDecimal(r.EstimatedValue),
            PurchaseDate   = TryDate(r.PurchaseDate),
            Notes          = r.Notes ?? "",
            Status         = r.Status ?? "Owned",
            Checklist      = ParseCsvChecklist(r.ChecklistItems)
        }).ToList();

        return await UpsertRowsAsync(rows);
    }

    // ── Core upsert ───────────────────────────────────────────────────────────

    private async Task<ImportResult> UpsertRowsAsync(List<ImportRow> rows)
    {
        var templates = await db.ChecklistTemplates.ToListAsync();
        var errors    = new List<string>();
        int created   = 0, updated = 0;

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

            var entries = templates
                .Where(t => t.ItemType == itemType)
                .OrderBy(t => t.SortOrder)
                .Select(t => new ChecklistEntry
                {
                    Label     = t.Label,
                    IsChecked = checkedLabels.Contains(t.Label.Trim().ToLowerInvariant()),
                    SortOrder = t.SortOrder
                })
                .ToList();

            foreach (var extra in row.Checklist.Where(c =>
                c.IsChecked &&
                !entries.Any(e => e.Label.Trim().ToLowerInvariant() == c.Label.Trim().ToLowerInvariant())))
            {
                entries.Add(new ChecklistEntry
                {
                    Label     = extra.Label,
                    IsChecked = true,
                    SortOrder = entries.Count + 1
                });
            }

            CollectionItem? existing = row.Id != Guid.Empty
                ? await db.Items.Include(i => i.ChecklistEntries)
                                .FirstOrDefaultAsync(i => i.Id == row.Id)
                : null;

            if (existing is not null)
            {
                existing.Name           = row.Name;
                existing.Type           = itemType.Value;
                existing.Platform       = row.Platform;
                existing.Region         = row.Region;
                existing.Condition      = row.Condition;
                existing.PurchasePrice  = row.PurchasePrice;
                existing.EstimatedValue = row.EstimatedValue;
                existing.PurchaseDate   = row.PurchaseDate.HasValue
                    ? DateOnly.FromDateTime(row.PurchaseDate.Value) : null;
                existing.Notes          = row.Notes;
                existing.Status         = itemStatus;
                existing.UpdatedAt      = DateTimeOffset.UtcNow;

                existing.ChecklistEntries.Clear();
                foreach (var e in entries) existing.ChecklistEntries.Add(e);

                updated++;
            }
            else
            {
                var item = new CollectionItem
                {
                    Id             = row.Id != Guid.Empty ? row.Id : Guid.NewGuid(),
                    Name           = row.Name,
                    Type           = itemType.Value,
                    Platform       = row.Platform,
                    Region         = row.Region,
                    Condition      = row.Condition,
                    PurchasePrice  = row.PurchasePrice,
                    EstimatedValue = row.EstimatedValue,
                    PurchaseDate   = row.PurchaseDate.HasValue
                        ? DateOnly.FromDateTime(row.PurchaseDate.Value) : null,
                    Notes            = row.Notes,
                    Status           = itemStatus,
                    ChecklistEntries = entries
                };
                db.Items.Add(item);
                created++;
            }
        }

        if (created > 0 || updated > 0)
            await db.SaveChangesAsync();

        return new ImportResult(created, updated, errors);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static ItemType? ParseItemType(string? raw) => raw?.Trim().ToLowerInvariant() switch
    {
        "game"    => ItemType.Game,
        "console" => ItemType.Console,
        _         => null
    };

    private static ItemStatus ParseItemStatus(string? raw) => raw?.Trim().ToLowerInvariant() switch
    {
        "wishlist" => ItemStatus.Wishlist,
        _          => ItemStatus.Owned
    };

    private static decimal? TryDecimal(string? s)
        => decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : null;

    private static DateTime? TryDate(string? s)
        => DateTime.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture,
            DateTimeStyles.None, out var d) ? d : null;

    private static List<ChecklistImportEntry> ParseCsvChecklist(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return [];
        return raw.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(part =>
            {
                var colon = part.LastIndexOf(':');
                if (colon < 0) return null;
                var label     = part[..colon].Trim();
                var isChecked = part[(colon + 1)..].Trim().ToLowerInvariant() == "yes";
                return string.IsNullOrEmpty(label) ? null : new ChecklistImportEntry(label, isChecked);
            })
            .Where(x => x is not null)
            .Select(x => x!)
            .ToList();
    }

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters                  = { new JsonStringEnumConverter() }
    };

    // ── Internal row models ───────────────────────────────────────────────────

    private class ImportRow
    {
        public Guid     Id             { get; set; }
        public string   Name           { get; set; } = "";
        public string   Type           { get; set; } = "";
        public string   Platform       { get; set; } = "";
        public string   Region         { get; set; } = "";
        public string   Condition      { get; set; } = "";
        public decimal? PurchasePrice  { get; set; }
        public decimal? EstimatedValue { get; set; }
        public DateTime? PurchaseDate  { get; set; }
        public string   Notes          { get; set; } = "";
        public string   Status         { get; set; } = "Owned";
        public List<ChecklistImportEntry> Checklist { get; set; } = [];
    }

    private class JsonImportRow
    {
        public Guid     Id             { get; set; }
        public string?  Name           { get; set; }
        public string?  Type           { get; set; }
        public string?  Platform       { get; set; }
        public string?  Region         { get; set; }
        public string?  Condition      { get; set; }
        public decimal? PurchasePrice  { get; set; }
        public decimal? EstimatedValue { get; set; }
        [JsonConverter(typeof(FlexibleDateConverter))]
        public DateTime? PurchaseDate  { get; set; }
        public string?  Notes          { get; set; }
        public string?  Status         { get; set; }
        public List<ChecklistImportEntry>? Checklist { get; set; }
    }

    private class CsvImportRow
    {
        public Guid    Id             { get; set; }
        public string? Name           { get; set; }
        public string? Type           { get; set; }
        public string? Platform       { get; set; }
        public string? Region         { get; set; }
        public string? Condition      { get; set; }
        public string? PurchasePrice  { get; set; }
        public string? EstimatedValue { get; set; }
        public string? PurchaseDate   { get; set; }
        public string? Status         { get; set; }
        public string? Notes          { get; set; }
        public string? ChecklistItems { get; set; }
        public string? CreatedAt      { get; set; }
    }
}

public record ChecklistImportEntry(string Label, bool IsChecked);

/// <summary>Handles both "yyyy-MM-dd" strings and full ISO-8601 date-time strings.</summary>
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
