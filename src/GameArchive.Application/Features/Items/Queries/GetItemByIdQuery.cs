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
        var i = await db.Items
            .Include(x => x.ChecklistEntries)
            .FirstOrDefaultAsync(x => x.Id == q.Id, ct);

        if (i is null) return null;

        return new CollectionItemDto(
            i.Id, i.Name, i.Type.ToString(), i.Platform, i.Region, i.Condition,
            i.PurchasePrice, i.EstimatedValue, i.PurchaseDate, i.Notes, i.Status.ToString(),
            i.ChecklistEntries.Select(e => new ChecklistEntryDto(e.Id, e.Label, e.IsChecked, e.SortOrder))
                              .OrderBy(e => e.SortOrder).ToList(),
            i.CreatedAt
        );
    }
}
