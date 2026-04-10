using GameArchive.Application.Common;
using GameArchive.Application.DTOs;
using GameArchive.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GameArchive.Application.Features.Items.Queries;

public record GetItemsQuery(
    string? Search,
    ItemType? Type,
    List<string>? Platforms,   // multi-select
    string? Condition,
    List<string>? Regions,     // multi-select
    ItemStatus? Status,
    string SortBy = "name",
    bool Descending = false,
    string? ChecklistFilter = null  // "cart-only" | "missing-box" | "missing-manual" | "complete" | "incomplete"
) : IRequest<List<CollectionItemDto>>;

public class GetItemsHandler(IAppDbContext db) : IRequestHandler<GetItemsQuery, List<CollectionItemDto>>
{
    public async Task<List<CollectionItemDto>> Handle(GetItemsQuery q, CancellationToken ct)
    {
        var query = db.Items
            .Include(i => i.ChecklistEntries)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q.Search))
            query = query.Where(i => i.Name.ToLower().Contains(q.Search.ToLower()));

        if (q.Type.HasValue)
            query = query.Where(i => i.Type == q.Type);

        if (q.Status.HasValue)
            query = query.Where(i => i.Status == q.Status);

        if (q.Platforms is { Count: > 0 })
            query = query.Where(i => q.Platforms.Contains(i.Platform));

        if (q.Regions is { Count: > 0 })
            query = query.Where(i => q.Regions.Contains(i.Region));

        if (!string.IsNullOrWhiteSpace(q.Condition))
            query = query.Where(i => i.Condition == q.Condition);

        var sortBy = q.SortBy.ToLowerInvariant();

        if (sortBy is "price" or "value")
        {
            var filteredItems = await query.ToListAsync(ct);
            filteredItems = ApplyChecklistFilter(filteredItems, q.ChecklistFilter);
            filteredItems = (sortBy, q.Descending) switch
            {
                ("price", false) => filteredItems.OrderBy(i => i.PurchasePrice ?? decimal.MaxValue)
                                                 .ThenBy(i => i.Name)
                                                 .ToList(),
                ("price", true)  => filteredItems.OrderByDescending(i => i.PurchasePrice ?? decimal.MinValue)
                                                 .ThenBy(i => i.Name)
                                                 .ToList(),
                ("value", false) => filteredItems.OrderBy(i => i.EstimatedValue ?? decimal.MaxValue)
                                                 .ThenBy(i => i.Name)
                                                 .ToList(),
                ("value", true)  => filteredItems.OrderByDescending(i => i.EstimatedValue ?? decimal.MinValue)
                                                 .ThenBy(i => i.Name)
                                                 .ToList(),
                _                => filteredItems
            };

            return filteredItems.Select(MapToDto).ToList();
        }

        query = (sortBy, q.Descending) switch
        {
            ("date", false)     => query.OrderBy(i => i.PurchaseDate),
            ("date", true)      => query.OrderByDescending(i => i.PurchaseDate),
            ("platform", false) => query.OrderBy(i => i.Platform).ThenBy(i => i.Name),
            ("platform", true)  => query.OrderByDescending(i => i.Platform).ThenBy(i => i.Name),
            (_,      false)     => query.OrderBy(i => i.Name),
            (_,      true)      => query.OrderByDescending(i => i.Name),
        };

        var items = await query.ToListAsync(ct);
        items = ApplyChecklistFilter(items, q.ChecklistFilter);
        return items.Select(MapToDto).ToList();
    }

    /// <summary>
    /// Filters items by checklist completeness. Evaluated in memory after DB query.
    /// Supported values:
    ///   "cart-only"      → solo tiene Cartucho/Disco (ni Caja ni Manual marcados)
    ///   "missing-box"    → tiene Cartucho pero le falta Caja
    ///   "missing-manual" → tiene Cartucho pero le falta Manual
    ///   "complete"       → todos los ítems del checklist marcados
    ///   "incomplete"     → al menos un ítem del checklist sin marcar
    /// </summary>
    private static List<Domain.Entities.CollectionItem> ApplyChecklistFilter(
        List<Domain.Entities.CollectionItem> items,
        string? filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
            return items;

        return filter switch
        {
            "cart-only" => items.Where(i =>
            {
                var entries = i.ChecklistEntries;
                if (entries.Count == 0) return false;
                var hasMedia = entries.Any(e => e.IsChecked && IsMedia(e.Label));
                var hasBox   = entries.Any(e => e.IsChecked && IsBox(e.Label));
                var hasManual = entries.Any(e => e.IsChecked && IsManual(e.Label));
                return hasMedia && !hasBox && !hasManual;
            }).ToList(),

            "missing-box" => items.Where(i =>
            {
                var entries = i.ChecklistEntries;
                if (entries.Count == 0) return false;
                var hasMedia = entries.Any(e => e.IsChecked && IsMedia(e.Label));
                var hasBox   = entries.Any(e => e.IsChecked && IsBox(e.Label));
                // Has cartridge/disc but no box
                return hasMedia && !hasBox;
            }).ToList(),

            "missing-manual" => items.Where(i =>
            {
                var entries = i.ChecklistEntries;
                if (entries.Count == 0) return false;
                var hasMedia  = entries.Any(e => e.IsChecked && IsMedia(e.Label));
                var hasManual = entries.Any(e => e.IsChecked && IsManual(e.Label));
                // Has cartridge/disc but no manual
                return hasMedia && !hasManual;
            }).ToList(),

            "complete" => items.Where(i =>
                i.ChecklistEntries.Count > 0 &&
                i.ChecklistEntries.All(e => e.IsChecked)
            ).ToList(),

            "incomplete" => items.Where(i =>
                i.ChecklistEntries.Count > 0 &&
                i.ChecklistEntries.Any(e => !e.IsChecked)
            ).ToList(),

            _ => items
        };
    }

    // Label helpers — match both English and Spanish labels used in the app
    private static bool IsMedia(string label)
    {
        var l = label.ToLowerInvariant();
        return l.Contains("cartucho") || l.Contains("disco") ||
               l.Contains("cartridge") || l.Contains("disc") || l.Contains("disk");
    }

    private static bool IsBox(string label)
    {
        var l = label.ToLowerInvariant();
        return l.Contains("caja") || l.Contains("box");
    }

    private static bool IsManual(string label)
        => label.ToLowerInvariant().Contains("manual");

    private static CollectionItemDto MapToDto(Domain.Entities.CollectionItem i) => new(
        i.Id, i.Name, i.Type.ToString(), i.Platform, i.Region, i.Condition,
        i.PurchasePrice, i.EstimatedValue, i.PurchaseDate, i.Notes, i.Status.ToString(),
        i.ChecklistEntries.Select(e => new ChecklistEntryDto(e.Id, e.Label, e.IsChecked, e.SortOrder))
                          .OrderBy(e => e.SortOrder).ToList(),
        i.CreatedAt,
        i.ProductUrl
    );
}
