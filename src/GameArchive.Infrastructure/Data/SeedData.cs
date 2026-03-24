using GameArchive.Domain;
using GameArchive.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GameArchive.Infrastructure.Data;

public static class SeedData
{
    public static async Task InitializeAsync(AppDbContext db)
    {
        await db.Database.MigrateAsync();

        if (await db.Items.AnyAsync()) return;

        var items = new List<CollectionItem>
        {
            new() {
                Name = "The Legend of Zelda: Ocarina of Time",
                Type = ItemType.Game, Platform = "Nintendo 64",
                Region = "NTSC-U", Condition = "Good",
                PurchasePrice = 45m, EstimatedValue = 80m,
                PurchaseDate = new DateOnly(2022, 3, 12),
                Status = ItemStatus.Owned,
                Notes = "CIB, slight wear on box corners"
            },
            new() {
                Name = "Super Mario World",
                Type = ItemType.Game, Platform = "SNES",
                Region = "PAL", Condition = "Good",
                PurchasePrice = 30m, EstimatedValue = 55m,
                PurchaseDate = new DateOnly(2021, 9, 20),
                Status = ItemStatus.Owned
            },
            new() {
                Name = "Super Nintendo Entertainment System",
                Type = ItemType.Console, Platform = "SNES",
                Region = "PAL", Condition = "Good",
                PurchasePrice = 120m, EstimatedValue = 180m,
                PurchaseDate = new DateOnly(2021, 7, 5),
                Status = ItemStatus.Owned,
                Notes = "Includes 2 controllers and power cable"
            },
            new() {
                Name = "Nintendo 64",
                Type = ItemType.Console, Platform = "Nintendo 64",
                Region = "NTSC-U", Condition = "Fair",
                PurchasePrice = 85m, EstimatedValue = 110m,
                PurchaseDate = new DateOnly(2020, 4, 15),
                Status = ItemStatus.Owned
            },
            new() {
                Name = "Chrono Trigger",
                Type = ItemType.Game, Platform = "SNES",
                Region = "NTSC-J", Condition = "Fair",
                PurchasePrice = null, EstimatedValue = 300m,
                Status = ItemStatus.Wishlist,
                Notes = "Dream item — Japanese cart version preferred"
            },
            new() {
                Name = "EarthBound",
                Type = ItemType.Game, Platform = "SNES",
                Region = "NTSC-U", Condition = "Good",
                PurchasePrice = null, EstimatedValue = 200m,
                Status = ItemStatus.Wishlist,
                Notes = "Complete in box would be ideal"
            }
        };

        db.Items.AddRange(items);
        await db.SaveChangesAsync();
    }
}
