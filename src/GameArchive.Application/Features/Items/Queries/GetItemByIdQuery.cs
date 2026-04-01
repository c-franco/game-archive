using GameArchive.Application.Common;
using GameArchive.Application.DTOs;
using GameArchive.Application.Features.Platforms;
using GameArchive.Application.Features.Regions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GameArchive.Application.Features.Items.Queries;

public record GetItemByIdQuery(Guid Id) : IRequest<CollectionItemDto?>;
public record GetItemEditContextQuery(Guid Id) : IRequest<ItemEditContextDto?>;
public record ItemEditContextDto(
    CollectionItemDto Item,
    List<PlatformDto> Platforms,
    List<RegionDto> Regions);

public class GetItemByIdHandler(IAppDbContext db) : IRequestHandler<GetItemByIdQuery, CollectionItemDto?>
{
    public async Task<CollectionItemDto?> Handle(GetItemByIdQuery q, CancellationToken ct)
    {
        return await db.Items
            .AsNoTracking()
            .Where(i => i.Id == q.Id)
            .Select(i => new CollectionItemDto(
                i.Id,
                i.Name,
                i.Type.ToString(),
                i.Platform,
                i.Region,
                i.Condition,
                i.PurchasePrice,
                i.EstimatedValue,
                i.PurchaseDate,
                i.Notes,
                i.Status.ToString(),
                i.ChecklistEntries
                    .OrderBy(e => e.SortOrder)
                    .Select(e => new ChecklistEntryDto(e.Id, e.Label, e.IsChecked, e.SortOrder))
                    .ToList(),
                i.CreatedAt))
            .FirstOrDefaultAsync(ct);
    }
}

public class GetItemEditContextHandler(IAppDbContext db) : IRequestHandler<GetItemEditContextQuery, ItemEditContextDto?>
{
    public async Task<ItemEditContextDto?> Handle(GetItemEditContextQuery q, CancellationToken ct)
    {
        var itemTask = db.Items
            .AsNoTracking()
            .Where(i => i.Id == q.Id)
            .Select(i => new CollectionItemDto(
                i.Id,
                i.Name,
                i.Type.ToString(),
                i.Platform,
                i.Region,
                i.Condition,
                i.PurchasePrice,
                i.EstimatedValue,
                i.PurchaseDate,
                i.Notes,
                i.Status.ToString(),
                i.ChecklistEntries
                    .OrderBy(e => e.SortOrder)
                    .Select(e => new ChecklistEntryDto(e.Id, e.Label, e.IsChecked, e.SortOrder))
                    .ToList(),
                i.CreatedAt))
            .FirstOrDefaultAsync(ct);

        var platformsTask = db.Platforms
            .AsNoTracking()
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Name)
            .Select(p => new PlatformDto(p.Id, p.Name, p.SortOrder))
            .ToListAsync(ct);

        var regionsTask = db.Regions
            .AsNoTracking()
            .OrderBy(r => r.SortOrder)
            .ThenBy(r => r.Name)
            .Select(r => new RegionDto(r.Id, r.Name, r.SortOrder))
            .ToListAsync(ct);

        await Task.WhenAll(itemTask, platformsTask, regionsTask);

        var item = itemTask.Result;
        if (item is null)
            return null;

        return new ItemEditContextDto(item, platformsTask.Result, regionsTask.Result);
    }
}
