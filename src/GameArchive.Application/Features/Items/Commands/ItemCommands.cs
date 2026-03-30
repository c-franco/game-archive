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
    ItemStatus Status
) : IRequest<Guid>;

public class CreateItemHandler(IAppDbContext db) : IRequestHandler<CreateItemCommand, Guid>
{
    public async Task<Guid> Handle(CreateItemCommand cmd, CancellationToken ct)
    {
        var item = new CollectionItem
        {
            Name           = cmd.Name,
            Type           = cmd.Type,
            Platform       = cmd.Platform,
            Region         = cmd.Region,
            Condition      = cmd.Condition,
            PurchasePrice  = cmd.PurchasePrice,
            EstimatedValue = cmd.EstimatedValue,
            PurchaseDate   = cmd.PurchaseDate,
            Notes          = cmd.Notes,
            Status         = cmd.Status
        };

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

        db.Items.Add(item);
        await db.SaveChangesAsync(ct);
        return item.Id;
    }
}

// ── UPDATE ──────────────────────────────────────────────────────────────────

public record ChecklistUpdate(Guid EntryId, bool IsChecked);

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

        item.Name           = cmd.Name;
        item.Type           = cmd.Type;
        item.Platform       = cmd.Platform;
        item.Region         = cmd.Region;
        item.Condition      = cmd.Condition;
        item.PurchasePrice  = cmd.PurchasePrice;
        item.EstimatedValue = cmd.EstimatedValue;
        item.PurchaseDate   = cmd.PurchaseDate;
        item.Notes          = cmd.Notes;
        item.Status         = cmd.Status;
        item.UpdatedAt      = DateTimeOffset.UtcNow;

        foreach (var update in cmd.ChecklistUpdates)
        {
            var entry = item.ChecklistEntries.FirstOrDefault(e => e.Id == update.EntryId);
            if (entry is not null) entry.IsChecked = update.IsChecked;
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
        var item = await db.Items.FindAsync([cmd.Id], ct)
            ?? throw new KeyNotFoundException(ServerStrings.Items.NotFoundFmt(cmd.Id));

        item.Status    = item.Status == ItemStatus.Owned ? ItemStatus.Wishlist : ItemStatus.Owned;
        item.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return item.Status.ToString();
    }
}
