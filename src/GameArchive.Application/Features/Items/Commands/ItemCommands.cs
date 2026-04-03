using GameArchive.Application.Common;
using GameArchive.Application.Resources;
using GameArchive.Domain;
using GameArchive.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GameArchive.Application.Features.Items.Commands;

// ── CREATE ──────────────────────────────────────────────────────────────────

public record CreateItemCommand(
    string Name,
    ItemType Type,
    string Platform,
    string Region,
    string Condition,
    decimal? PurchasePrice,
    decimal? EstimatedValue,
    DateOnly? PurchaseDate,
    string Notes,
    ItemStatus Status,
    List<ChecklistUpdate> ChecklistUpdates
) : IRequest<Guid>;

public class CreateItemHandler(IAppDbContext db) : IRequestHandler<CreateItemCommand, Guid>
{
    public async Task<Guid> Handle(CreateItemCommand cmd, CancellationToken ct)
    {
        var purchasePrice = cmd.Status == ItemStatus.Wishlist ? null : cmd.PurchasePrice;
        var purchaseDate  = cmd.Status == ItemStatus.Wishlist ? null : cmd.PurchaseDate;

        var item = new CollectionItem
        {
            Name           = cmd.Name,
            Type           = cmd.Type,
            Platform       = cmd.Platform,
            Region         = cmd.Region,
            Condition      = cmd.Condition,
            PurchasePrice  = purchasePrice,
            EstimatedValue = cmd.EstimatedValue,
            PurchaseDate   = purchaseDate,
            Notes          = cmd.Notes,
            Status         = cmd.Status
        };

        if (cmd.Status == ItemStatus.Owned)
        {
            var templates = await db.ChecklistTemplates
                .Where(t => t.ItemType == cmd.Type)
                .OrderBy(t => t.SortOrder)
                .ToListAsync(ct);

            item.ChecklistEntries = templates.Select(t => new ChecklistEntry
            {
                Label     = t.Label,
                IsChecked = cmd.ChecklistUpdates?.FirstOrDefault(u => u.Label == t.Label)?.IsChecked ?? false,
                SortOrder = t.SortOrder
            }).ToList();
        }

        db.Items.Add(item);
        await db.SaveChangesAsync(ct);
        return item.Id;
    }
}

// ── UPDATE ──────────────────────────────────────────────────────────────────

public record ChecklistUpdate(string Label, bool IsChecked);

public record UpdateItemCommand(
    Guid Id,
    string Name,
    ItemType Type,
    string Platform,
    string Region,
    string Condition,
    decimal? PurchasePrice,
    decimal? EstimatedValue,
    DateOnly? PurchaseDate,
    string Notes,
    ItemStatus Status,
    List<ChecklistUpdate> ChecklistUpdates
) : IRequest;

public class UpdateItemHandler(IAppDbContext db) : IRequestHandler<UpdateItemCommand>
{
    public async Task Handle(UpdateItemCommand cmd, CancellationToken ct)
    {
        var item = await db.Items
            .Include(i => i.ChecklistEntries)
            .FirstOrDefaultAsync(i => i.Id == cmd.Id, ct)
            ?? throw new KeyNotFoundException(ServerStrings.Items.NotFoundFmt(cmd.Id));

        var purchasePrice = cmd.Status == ItemStatus.Wishlist ? null : cmd.PurchasePrice;
        var purchaseDate  = cmd.Status == ItemStatus.Wishlist ? null : cmd.PurchaseDate;

        item.Name           = cmd.Name;
        item.Type           = cmd.Type;
        item.Platform       = cmd.Platform;
        item.Region         = cmd.Region;
        item.Condition      = cmd.Condition;
        item.PurchasePrice  = purchasePrice;
        item.EstimatedValue = cmd.EstimatedValue;
        item.PurchaseDate   = purchaseDate;
        item.Notes          = cmd.Notes;
        item.Status         = cmd.Status;
        item.UpdatedAt      = DateTimeOffset.UtcNow;

        if (cmd.Status == ItemStatus.Wishlist)
        {
            item.ChecklistEntries.Clear();
        }
        else
        {
            if (item.ChecklistEntries.Count == 0)
            {
                var templates = await db.ChecklistTemplates
                    .Where(t => t.ItemType == cmd.Type)
                    .OrderBy(t => t.SortOrder)
                    .ToListAsync(ct);

                item.ChecklistEntries = templates.Select(t => new ChecklistEntry
                {
                    Label     = t.Label,
                    IsChecked = false,
                    SortOrder = t.SortOrder
                }).ToList();
            }

            foreach (var update in cmd.ChecklistUpdates)
            {
                var entry = item.ChecklistEntries.FirstOrDefault(e => e.Label == update.Label);
                if (entry is not null) entry.IsChecked = update.IsChecked;
            }
        }

        await db.SaveChangesAsync(ct);
    }
}

// ── DELETE ──────────────────────────────────────────────────────────────────

public record DeleteItemCommand(Guid Id) : IRequest;

public class DeleteItemHandler(IAppDbContext db) : IRequestHandler<DeleteItemCommand>
{
    public async Task Handle(DeleteItemCommand cmd, CancellationToken ct)
    {
        var item = await db.Items.FindAsync([cmd.Id], ct)
            ?? throw new KeyNotFoundException(ServerStrings.Items.NotFoundFmt(cmd.Id));

        db.Items.Remove(item);
        await db.SaveChangesAsync(ct);
    }
}

// ── TOGGLE STATUS ────────────────────────────────────────────────────────────

public record ToggleStatusCommand(Guid Id) : IRequest<string>;

public class ToggleStatusHandler(IAppDbContext db) : IRequestHandler<ToggleStatusCommand, string>
{
    public async Task<string> Handle(ToggleStatusCommand cmd, CancellationToken ct)
    {
        var item = await db.Items
            .Include(i => i.ChecklistEntries)
            .FirstOrDefaultAsync(i => i.Id == cmd.Id, ct)
            ?? throw new KeyNotFoundException(ServerStrings.Items.NotFoundFmt(cmd.Id));

        item.Status = item.Status == ItemStatus.Owned ? ItemStatus.Wishlist : ItemStatus.Owned;

        if (item.Status == ItemStatus.Wishlist)
        {
            item.PurchasePrice = null;
            item.PurchaseDate = null;
            item.ChecklistEntries.Clear();
        }
        else if (item.ChecklistEntries.Count == 0)
        {
            var templates = await db.ChecklistTemplates
                .Where(t => t.ItemType == item.Type)
                .OrderBy(t => t.SortOrder)
                .ToListAsync(ct);

            item.ChecklistEntries = templates.Select(t => new ChecklistEntry
            {
                Label     = t.Label,
                IsChecked = false,
                SortOrder = t.SortOrder
            }).ToList();
        }

        item.UpdatedAt = DateTimeOffset.UtcNow;
        try
        {
            await db.SaveChangesAsync(ct);
            return item.Status.ToString();
        }
        catch (DbUpdateConcurrencyException)
        {
            // If another request changed/deleted the row first, return the current persisted status.
            var current = await db.Items
                .AsNoTracking()
                .Where(i => i.Id == cmd.Id)
                .Select(i => (ItemStatus?)i.Status)
                .FirstOrDefaultAsync(ct);

            if (current is null)
                throw new KeyNotFoundException(ServerStrings.Items.NotFoundFmt(cmd.Id));

            return current.Value.ToString();
        }
    }
}

// ── MARK AS OWNED (IDEMPOTENT) ───────────────────────────────────────────────

public record MarkAsOwnedCommand(Guid Id) : IRequest<string>;

public class MarkAsOwnedHandler(IAppDbContext db) : IRequestHandler<MarkAsOwnedCommand, string>
{
    public async Task<string> Handle(MarkAsOwnedCommand cmd, CancellationToken ct)
    {
        var itemInfo = await db.Items
            .AsNoTracking()
            .Where(i => i.Id == cmd.Id)
            .Select(i => new { i.Status, i.Type })
            .FirstOrDefaultAsync(ct);

        if (itemInfo is null)
            throw new KeyNotFoundException(ServerStrings.Items.NotFoundFmt(cmd.Id));

        if (itemInfo.Status != ItemStatus.Owned)
        {
            var now = DateTimeOffset.UtcNow;
            var affected = await db.Items
                .Where(i => i.Id == cmd.Id && i.Status != ItemStatus.Owned)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(i => i.Status, i => ItemStatus.Owned)
                    .SetProperty(i => i.UpdatedAt, i => now), ct);

            if (affected == 0)
            {
                // If no row was affected, return the final persisted state (or 404 if deleted).
                var current = await db.Items
                    .AsNoTracking()
                    .Where(i => i.Id == cmd.Id)
                    .Select(i => (ItemStatus?)i.Status)
                    .FirstOrDefaultAsync(ct);

                if (current is null)
                    throw new KeyNotFoundException(ServerStrings.Items.NotFoundFmt(cmd.Id));

                if (current != ItemStatus.Owned)
                    return current.Value.ToString();
            }
        }

        // Ensure an owned item always has its checklist entries.
        var hasChecklist = await db.ChecklistEntries
            .AsNoTracking()
            .AnyAsync(e => e.CollectionItemId == cmd.Id, ct);

        if (!hasChecklist)
        {
            var templates = await db.ChecklistTemplates
                .AsNoTracking()
                .Where(t => t.ItemType == itemInfo.Type)
                .OrderBy(t => t.SortOrder)
                .ToListAsync(ct);

            if (templates.Count > 0)
            {
                db.ChecklistEntries.AddRange(templates.Select(t => new ChecklistEntry
                {
                    CollectionItemId = cmd.Id,
                    Label = t.Label,
                    IsChecked = false,
                    SortOrder = t.SortOrder
                }));

                await db.SaveChangesAsync(ct);
            }
        }

        return ItemStatus.Owned.ToString();
    }
}
