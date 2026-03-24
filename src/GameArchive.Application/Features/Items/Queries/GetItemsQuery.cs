using GameArchive.Application.Common;
using GameArchive.Application.DTOs;
using GameArchive.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GameArchive.Application.Features.Items.Queries;

public record GetItemsQuery(
    string? Search,
    ItemType? Type,
    string? Platform,
    string? Condition,
    string? Region,
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

        if (q.Type.HasValue)        query = query.Where(i => i.Type == q.Type);
        if (q.Status.HasValue)      query = query.Where(i => i.Status == q.Status);
        if (!string.IsNullOrWhiteSpace(q.Platform))  query = query.Where(i => i.Platform == q.Platform);
        if (!string.IsNullOrWhiteSpace(q.Condition)) query = query.Where(i => i.Condition == q.Condition);
        if (!string.IsNullOrWhiteSpace(q.Region))    query = query.Where(i => i.Region == q.Region);

        query = (q.SortBy.ToLower(), q.Descending) switch
        {
            ("price", false) => query.OrderBy(i => i.PurchasePrice),
            ("price", true)  => query.OrderByDescending(i => i.PurchasePrice),
            ("value", false) => query.OrderBy(i => i.EstimatedValue),
            ("value", true)  => query.OrderByDescending(i => i.EstimatedValue),
            ("date",  false) => query.OrderBy(i => i.PurchaseDate),
            ("date",  true)  => query.OrderByDescending(i => i.PurchaseDate),
            (_,       false) => query.OrderBy(i => i.Name),
            (_,       true)  => query.OrderByDescending(i => i.Name),
        };

        var items = await query.ToListAsync(ct);
        return items.Select(MapToDto).ToList();
    }

    private static CollectionItemDto MapToDto(Domain.Entities.CollectionItem i) => new(
        i.Id, i.Name, i.Type.ToString(), i.Platform, i.Region, i.Condition,
        i.PurchasePrice, i.EstimatedValue, i.PurchaseDate, i.Notes, i.Status.ToString(),
        i.ChecklistEntries.Select(e => new ChecklistEntryDto(e.Id, e.Label, e.IsChecked, e.SortOrder))
                          .OrderBy(e => e.SortOrder).ToList(),
        i.CreatedAt
    );
}
