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
    bool Descending = false
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
            ("date", false) => query.OrderBy(i => i.PurchaseDate),
            ("date", true)  => query.OrderByDescending(i => i.PurchaseDate),
            (_,      false) => query.OrderBy(i => i.Name),
            (_,      true)  => query.OrderByDescending(i => i.Name),
        };

        var items = await query.ToListAsync(ct);
        return items.Select(MapToDto).ToList();
    }

    private static CollectionItemDto MapToDto(Domain.Entities.CollectionItem i) => new(
        i.Id, i.Name, i.Type.ToString(), i.Platform, i.Region, i.Condition,
        i.PurchasePrice, i.EstimatedValue, i.PurchaseDate, i.Notes, i.Status.ToString(),
        i.ChecklistEntries.Select(e => new ChecklistEntryDto(e.Id, e.Label, e.IsChecked, e.SortOrder))
                          .OrderBy(e => e.SortOrder).ToList(),
        i.CreatedAt,
        i.ProductUrl
    );
}
