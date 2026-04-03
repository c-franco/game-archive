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
    decimal TotalWishlistValue,
    Dictionary<string, int> ByType,
    Dictionary<string, int> ByPlatform
);

public record GetStatsQuery : IRequest<StatsDto>;

public class GetStatsHandler(IAppDbContext db) : IRequestHandler<GetStatsQuery, StatsDto>
{
    public async Task<StatsDto> Handle(GetStatsQuery _, CancellationToken ct)
    {
        var items = await db.Items.ToListAsync(ct);

        var owned    = items.Where(i => i.Status == ItemStatus.Owned).ToList();
        var wishlist = items.Where(i => i.Status == ItemStatus.Wishlist).ToList();

        return new StatsDto(
            TotalItems:          items.Count,
            TotalOwned:          owned.Count,
            TotalWishlist:       wishlist.Count,
            TotalEstimatedValue: owned
                                     .Where(i => i.EstimatedValue.HasValue)
                                     .Sum(i => i.EstimatedValue!.Value),
            TotalSpent:          owned
                                     .Where(i => i.PurchasePrice.HasValue)
                                     .Sum(i => i.PurchasePrice!.Value),
            TotalWishlistValue:  wishlist
                                     .Where(i => i.EstimatedValue.HasValue)
                                     .Sum(i => i.EstimatedValue!.Value),
            ByType:              owned
                                     .GroupBy(i => i.Type.ToString())
                                     .ToDictionary(g => g.Key, g => g.Count()),
            ByPlatform:          owned
                                     .Where(i => !string.IsNullOrEmpty(i.Platform))
                                     .GroupBy(i => i.Platform)
                                     .ToDictionary(g => g.Key, g => g.Count())
        );
    }
}
