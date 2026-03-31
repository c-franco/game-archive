using GameArchive.Application.Common;
using GameArchive.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GameArchive.Application.Features.Stats;

public record StatsDto(
    int TotalItems,
    int TotalOwned,
    int TotalWishlist,
    decimal TotalEstimatedValue,
    decimal TotalSpent,
    Dictionary<string, int> ByType,
    Dictionary<string, int> ByPlatform
);

public record GetStatsQuery : IRequest<StatsDto>;

public class GetStatsHandler(IAppDbContext db) : IRequestHandler<GetStatsQuery, StatsDto>
{
    public async Task<StatsDto> Handle(GetStatsQuery _, CancellationToken ct)
    {
        var items = await db.Items.ToListAsync(ct);

        return new StatsDto(
            TotalItems:          items.Count,
            TotalOwned:          items.Count(i => i.Status == ItemStatus.Owned),
            TotalWishlist:       items.Count(i => i.Status == ItemStatus.Wishlist),
            TotalEstimatedValue: items.Where(i => i.EstimatedValue.HasValue && i.Status == ItemStatus.Owned)
                                      .Sum(i => i.EstimatedValue!.Value),
            TotalSpent:          items.Where(i => i.PurchasePrice.HasValue && i.Status == ItemStatus.Owned).Sum(i => i.PurchasePrice!.Value),
            ByType:              items.GroupBy(i => i.Type.ToString()).ToDictionary(g => g.Key, g => g.Count()),
            ByPlatform:          items.Where(i => !string.IsNullOrEmpty(i.Platform))
                                      .GroupBy(i => i.Platform).ToDictionary(g => g.Key, g => g.Count())
        );
    }
}
