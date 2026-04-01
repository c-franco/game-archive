using GameArchive.Application.Common;
using GameArchive.Application.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GameArchive.Application.Features.Items.Queries;

public record GetItemByIdQuery(Guid Id) : IRequest<CollectionItemDto?>;

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
