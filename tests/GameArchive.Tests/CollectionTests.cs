using FluentAssertions;
using GameArchive.Application.Features.Items.Commands;
using GameArchive.Application.Features.Items.Queries;
using GameArchive.Application.Features.Stats;
using GameArchive.Domain;
using GameArchive.Domain.Entities;
using GameArchive.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GameArchive.Tests;

// ── Helpers ──────────────────────────────────────────────────────────────────

file static class DbFactory
{
    public static AppDbContext Create()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(opts);
    }
}

// ── GetItemsHandler Tests ─────────────────────────────────────────────────────

public class GetItemsHandlerTests
{
    [Fact]
    public async Task Returns_all_items_when_no_filters()
    {
        var db = DbFactory.Create();
        db.Items.AddRange(
            new CollectionItem { Name = "Zelda", Type = ItemType.Game },
            new CollectionItem { Name = "SNES",  Type = ItemType.Console }
        );
        await db.SaveChangesAsync();

        var result = await new GetItemsHandler(db)
            .Handle(new GetItemsQuery(null, null, null, null, null, null), default);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task Filters_by_status_owned()
    {
        var db = DbFactory.Create();
        db.Items.AddRange(
            new CollectionItem { Name = "Owned Game",   Type = ItemType.Game, Status = ItemStatus.Owned },
            new CollectionItem { Name = "Wishlist Game", Type = ItemType.Game, Status = ItemStatus.Wishlist }
        );
        await db.SaveChangesAsync();

        var result = await new GetItemsHandler(db)
            .Handle(new GetItemsQuery(null, null, null, null, null, ItemStatus.Owned), default);

        result.Should().ContainSingle(i => i.Name == "Owned Game");
    }

    [Fact]
    public async Task Filters_by_status_wishlist()
    {
        var db = DbFactory.Create();
        db.Items.AddRange(
            new CollectionItem { Name = "Owned",    Type = ItemType.Game, Status = ItemStatus.Owned },
            new CollectionItem { Name = "Chrono Trigger", Type = ItemType.Game, Status = ItemStatus.Wishlist }
        );
        await db.SaveChangesAsync();

        var result = await new GetItemsHandler(db)
            .Handle(new GetItemsQuery(null, null, null, null, null, ItemStatus.Wishlist), default);

        result.Should().ContainSingle(i => i.Name == "Chrono Trigger");
    }

    [Fact]
    public async Task Search_is_case_insensitive()
    {
        var db = DbFactory.Create();
        db.Items.AddRange(
            new CollectionItem { Name = "The Legend of Zelda", Type = ItemType.Game },
            new CollectionItem { Name = "Super Mario World",   Type = ItemType.Game }
        );
        await db.SaveChangesAsync();

        var result = await new GetItemsHandler(db)
            .Handle(new GetItemsQuery("ZELDA", null, null, null, null, null), default);

        result.Should().ContainSingle();
        result[0].Name.Should().Contain("Zelda");
    }

    [Fact]
    public async Task Filters_by_type_game()
    {
        var db = DbFactory.Create();
        db.Items.AddRange(
            new CollectionItem { Name = "Mario 64",  Type = ItemType.Game },
            new CollectionItem { Name = "N64",       Type = ItemType.Console }
        );
        await db.SaveChangesAsync();

        var result = await new GetItemsHandler(db)
            .Handle(new GetItemsQuery(null, ItemType.Game, null, null, null, null), default);

        result.Should().ContainSingle(i => i.Type == "Game");
    }

    [Fact]
    public async Task Filters_by_platform()
    {
        var db = DbFactory.Create();
        db.Items.AddRange(
            new CollectionItem { Name = "Zelda",  Type = ItemType.Game, Platform = "Nintendo 64" },
            new CollectionItem { Name = "FF VII", Type = ItemType.Game, Platform = "PlayStation" }
        );
        await db.SaveChangesAsync();

        var result = await new GetItemsHandler(db)
            .Handle(new GetItemsQuery(null, null, "Nintendo 64", null, null, null), default);

        result.Should().ContainSingle(i => i.Platform == "Nintendo 64");
    }

    [Fact]
    public async Task Sorts_by_name_ascending_by_default()
    {
        var db = DbFactory.Create();
        db.Items.AddRange(
            new CollectionItem { Name = "Zelda",  Type = ItemType.Game },
            new CollectionItem { Name = "Asteroids", Type = ItemType.Game },
            new CollectionItem { Name = "Mario",  Type = ItemType.Game }
        );
        await db.SaveChangesAsync();

        var result = await new GetItemsHandler(db)
            .Handle(new GetItemsQuery(null, null, null, null, null, null, "name", false), default);

        result.Select(i => i.Name).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task Returns_empty_list_when_no_items()
    {
        var db = DbFactory.Create();
        var result = await new GetItemsHandler(db)
            .Handle(new GetItemsQuery(null, null, null, null, null, null), default);
        result.Should().BeEmpty();
    }
}

// ── CreateItemHandler Tests ───────────────────────────────────────────────────

public class CreateItemHandlerTests
{
    [Fact]
    public async Task Creates_item_and_returns_guid()
    {
        var db = DbFactory.Create();
        var cmd = new CreateItemCommand("Donkey Kong Country", ItemType.Game, "SNES", "PAL", "Good", 20m, 40m, null, "", ItemStatus.Owned);

        var id = await new CreateItemHandler(db).Handle(cmd, default);

        id.Should().NotBeEmpty();
        var saved = await db.Items.FindAsync(id);
        saved.Should().NotBeNull();
        saved!.Name.Should().Be("Donkey Kong Country");
    }

    [Fact]
    public async Task Auto_seeds_checklist_from_templates()
    {
        var db = DbFactory.Create();
        db.ChecklistTemplates.AddRange(
            new ChecklistTemplate { ItemType = ItemType.Game, Label = "Box",    SortOrder = 1 },
            new ChecklistTemplate { ItemType = ItemType.Game, Label = "Manual", SortOrder = 2 }
        );
        await db.SaveChangesAsync();

        var cmd = new CreateItemCommand("Metroid", ItemType.Game, "NES", "NTSC-U", "Fair", null, null, null, "", ItemStatus.Owned);
        var id = await new CreateItemHandler(db).Handle(cmd, default);

        var item = await db.Items.Include(i => i.ChecklistEntries).FirstAsync(i => i.Id == id);
        item.ChecklistEntries.Should().HaveCount(2);
        item.ChecklistEntries.Select(e => e.Label).Should().Contain(new[] { "Box", "Manual" });
        item.ChecklistEntries.All(e => !e.IsChecked).Should().BeTrue();
    }

    [Fact]
    public async Task Does_not_seed_checklist_for_wrong_type()
    {
        var db = DbFactory.Create();
        db.ChecklistTemplates.Add(
            new ChecklistTemplate { ItemType = ItemType.Game, Label = "Box", SortOrder = 1 }
        );
        await db.SaveChangesAsync();

        var cmd = new CreateItemCommand("Super NES", ItemType.Console, "SNES", "PAL", "Good", null, null, null, "", ItemStatus.Owned);
        var id = await new CreateItemHandler(db).Handle(cmd, default);

        var item = await db.Items.Include(i => i.ChecklistEntries).FirstAsync(i => i.Id == id);
        item.ChecklistEntries.Should().BeEmpty();
    }
}

// ── DeleteItemHandler Tests ───────────────────────────────────────────────────

public class DeleteItemHandlerTests
{
    [Fact]
    public async Task Deletes_item_from_database()
    {
        var db = DbFactory.Create();
        var item = new CollectionItem { Name = "ToDelete", Type = ItemType.Game };
        db.Items.Add(item);
        await db.SaveChangesAsync();

        await new DeleteItemHandler(db).Handle(new DeleteItemCommand(item.Id), default);

        db.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Throws_when_item_not_found()
    {
        var db = DbFactory.Create();
        var act = async () => await new DeleteItemHandler(db).Handle(new DeleteItemCommand(Guid.NewGuid()), default);
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}

// ── ToggleStatusHandler Tests ─────────────────────────────────────────────────

public class ToggleStatusHandlerTests
{
    [Fact]
    public async Task Toggles_owned_to_wishlist()
    {
        var db = DbFactory.Create();
        var item = new CollectionItem { Name = "Mario", Type = ItemType.Game, Status = ItemStatus.Owned };
        db.Items.Add(item);
        await db.SaveChangesAsync();

        var result = await new ToggleStatusHandler(db).Handle(new ToggleStatusCommand(item.Id), default);

        result.Should().Be("Wishlist");
        (await db.Items.FindAsync(item.Id))!.Status.Should().Be(ItemStatus.Wishlist);
    }

    [Fact]
    public async Task Toggles_wishlist_to_owned()
    {
        var db = DbFactory.Create();
        var item = new CollectionItem { Name = "Chrono Trigger", Type = ItemType.Game, Status = ItemStatus.Wishlist };
        db.Items.Add(item);
        await db.SaveChangesAsync();

        var result = await new ToggleStatusHandler(db).Handle(new ToggleStatusCommand(item.Id), default);

        result.Should().Be("Owned");
    }
}

// ── GetStatsHandler Tests ─────────────────────────────────────────────────────

public class GetStatsHandlerTests
{
    [Fact]
    public async Task Calculates_stats_correctly()
    {
        var db = DbFactory.Create();
        db.Items.AddRange(
            new CollectionItem { Name = "A", Type = ItemType.Game,    Platform = "SNES",         Status = ItemStatus.Owned,    PurchasePrice = 30m, EstimatedValue = 50m },
            new CollectionItem { Name = "B", Type = ItemType.Game,    Platform = "Nintendo 64",  Status = ItemStatus.Owned,    PurchasePrice = 20m, EstimatedValue = 80m },
            new CollectionItem { Name = "C", Type = ItemType.Console, Platform = "SNES",         Status = ItemStatus.Wishlist, PurchasePrice = null, EstimatedValue = 150m }
        );
        await db.SaveChangesAsync();

        var stats = await new GetStatsHandler(db).Handle(new GetStatsQuery(), default);

        stats.TotalItems.Should().Be(3);
        stats.TotalOwned.Should().Be(2);
        stats.TotalWishlist.Should().Be(1);
        stats.TotalEstimatedValue.Should().Be(280m);
        stats.TotalSpent.Should().Be(50m);
        stats.ByType.Should().ContainKey("Game").WhoseValue.Should().Be(2);
        stats.ByType.Should().ContainKey("Console").WhoseValue.Should().Be(1);
        stats.ByPlatform["SNES"].Should().Be(2);
    }

    [Fact]
    public async Task Returns_zero_stats_for_empty_collection()
    {
        var db = DbFactory.Create();
        var stats = await new GetStatsHandler(db).Handle(new GetStatsQuery(), default);
        stats.TotalItems.Should().Be(0);
        stats.TotalEstimatedValue.Should().Be(0);
    }
}
