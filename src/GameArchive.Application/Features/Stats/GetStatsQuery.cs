using GameArchive.Application.Common;
using GameArchive.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GameArchive.Application.Features.Stats;

public record PlatformValueDto(
    string Platform,
    int Count,
    decimal Spent,
    decimal EstimatedValue
);

public record StatsDto(
    int TotalItems,
    int TotalOwned,
    int TotalWishlist,
    decimal TotalEstimatedValue,
    decimal TotalSpent,
    decimal TotalWishlistValue,
    Dictionary<string, int> ByType,
    Dictionary<string, int> ByPlatform,
    List<PlatformValueDto> PlatformValues
);

public record GetStatsQuery : IRequest<StatsDto>;

public class GetStatsHandler(IAppDbContext db) : IRequestHandler<GetStatsQuery, StatsDto>
{
    public async Task<StatsDto> Handle(GetStatsQuery _, CancellationToken ct)
    {
        var items = await db.Items.ToListAsync(ct);

        var owned    = items.Where(i => i.Status == ItemStatus.Owned).ToList();
        var wishlist = items.Where(i => i.Status == ItemStatus.Wishlist).ToList();

        var platformValues = owned
            .Where(i => !string.IsNullOrEmpty(i.Platform))
            .GroupBy(i => i.Platform)
            .Select(g => new PlatformValueDto(
                Platform: g.Key,
                Count: g.Count(),
                Spent: g.Where(i => i.PurchasePrice.HasValue).Sum(i => i.PurchasePrice!.Value),
                EstimatedValue: g.Where(i => i.EstimatedValue.HasValue).Sum(i => i.EstimatedValue!.Value)
            ))
            .OrderByDescending(p => p.EstimatedValue - p.Spent)
            .ToList();

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
                                     .ToDictionary(g => g.Key, g => g.Count()),
            PlatformValues:      platformValues
        );
    }
}
