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

        var now = DateTimeOffset.UtcNow;

        var items = new List<CollectionItem>
        {
            new()
            {
                Name = "The Legend of Zelda: Ocarina of Time",
                Type = ItemType.Game,
                Platform = "Nintendo 64",
                Region = "NTSC-U",
                Condition = "Good",
                PurchasePrice = 45m,
                EstimatedValue = 80m,
                PurchaseDate = new DateOnly(2022, 3, 12),
                Status = ItemStatus.Owned,
                Notes = "CIB, slight wear on box corners",
                CreatedAt = now,
                UpdatedAt = now,
                PriceSource = "Manual",
                PriceLastFetchedAt = now,
                ChecklistEntries = new List<ChecklistEntry>
                {
                    new() { Label = "Caja", IsChecked = true, SortOrder = 1 },
                    new() { Label = "Manual", IsChecked = true, SortOrder = 2 },
                    new() { Label = "Cartucho/Disco", IsChecked = true, SortOrder = 3 }
                }
            },
            new()
            {
                Name = "Super Mario World",
                Type = ItemType.Game,
                Platform = "SNES",
                Region = "PAL",
                Condition = "Good",
                PurchasePrice = 30m,
                EstimatedValue = 55m,
                PurchaseDate = new DateOnly(2021, 9, 20),
                Status = ItemStatus.Owned,
                CreatedAt = now,
                UpdatedAt = now,
                PriceSource = "Manual",
                PriceLastFetchedAt = now,
                ChecklistEntries = new List<ChecklistEntry>
                {
                    new() { Label = "Caja", IsChecked = false, SortOrder = 1 },
                    new() { Label = "Manual", IsChecked = false, SortOrder = 2 },
                    new() { Label = "Cartucho/Disco", IsChecked = true, SortOrder = 3 }
                }
            },
            new()
            {
                Name = "Super Nintendo Entertainment System",
                Type = ItemType.Console,
                Platform = "SNES",
                Region = "PAL",
                Condition = "Good",
                PurchasePrice = 120m,
                EstimatedValue = 180m,
                PurchaseDate = new DateOnly(2021, 7, 5),
                Status = ItemStatus.Owned,
                Notes = "Includes 2 controllers and power cable",
                CreatedAt = now,
                UpdatedAt = now,
                PriceSource = "Manual",
                PriceLastFetchedAt = now,
                ChecklistEntries = new List<ChecklistEntry>
                {
                    new() { Label = "Caja", IsChecked = true, SortOrder = 1 },
                    new() { Label = "Mando", IsChecked = true, SortOrder = 2 },
                    new() { Label = "Cables", IsChecked = true, SortOrder = 3 },
                    new() { Label = "Manual", IsChecked = true, SortOrder = 4 }
                }
            },
            new()
            {
                Name = "Nintendo 64",
                Type = ItemType.Console,
                Platform = "Nintendo 64",
                Region = "NTSC-U",
                Condition = "Fair",
                PurchasePrice = 85m,
                EstimatedValue = 110m,
                PurchaseDate = new DateOnly(2020, 4, 15),
                Status = ItemStatus.Owned,
                CreatedAt = now,
                UpdatedAt = now,
                PriceSource = "Manual",
                PriceLastFetchedAt = now,
                ChecklistEntries = new List<ChecklistEntry>
                {
                    new() { Label = "Caja", IsChecked = true, SortOrder = 1 },
                    new() { Label = "Mando", IsChecked = true, SortOrder = 2 },
                    new() { Label = "Cables", IsChecked = false, SortOrder = 3 },
                    new() { Label = "Manual", IsChecked = false, SortOrder = 4 }
                }
            },
            new()
            {
                Name = "Chrono Trigger",
                Type = ItemType.Game,
                Platform = "SNES",
                Region = "NTSC-J",
                Condition = "Fair",
                PurchasePrice = null,
                EstimatedValue = 300m,
                Status = ItemStatus.Wishlist,
                Notes = "Dream item — Japanese cart version preferred",
                CreatedAt = now,
                UpdatedAt = now,
                PriceSource = "PriceCharting",
                PriceLastFetchedAt = now,
                ChecklistEntries = new List<ChecklistEntry>
                {
                    new() { Label = "Caja", IsChecked = false, SortOrder = 1 },
                    new() { Label = "Manual", IsChecked = false, SortOrder = 2 },
                    new() { Label = "Cartucho/Disco", IsChecked = false, SortOrder = 3 }
                }
            },
            new()
            {
                Name = "EarthBound",
                Type = ItemType.Game,
                Platform = "SNES",
                Region = "NTSC-U",
                Condition = "Good",
                PurchasePrice = null,
                EstimatedValue = 200m,
                Status = ItemStatus.Wishlist,
                Notes = "Complete in box would be ideal",
                CreatedAt = now,
                UpdatedAt = now,
                PriceSource = "PriceCharting",
                PriceLastFetchedAt = now,
                ChecklistEntries = new List<ChecklistEntry>
                {
                    new() { Label = "Caja", IsChecked = false, SortOrder = 1 },
                    new() { Label = "Manual", IsChecked = false, SortOrder = 2 },
                    new() { Label = "Cartucho/Disco", IsChecked = false, SortOrder = 3 }
                }
            }
        };

        db.Items.AddRange(items);
        await db.SaveChangesAsync();
    }
}