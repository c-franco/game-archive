using GameArchive.Application.Common;
using GameArchive.Application.DTOs;
using GameArchive.Application.Features.Platforms;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GameArchive.Application.Features.Settings;

// ── DTOs ─────────────────────────────────────────────────────────────────────

public record SettingsDto(
    List<ChecklistTemplateDto> Templates,
    List<PlatformDto>          Platforms
);

public record TemplateSaveDto(Guid? Id, string ItemType, string Label, int SortOrder);
public record PlatformSaveDto(string Name, int SortOrder);

public record SaveSettingsCommand(
    List<TemplateSaveDto> Templates,
    List<PlatformSaveDto> Platforms
) : IRequest;

// ── GET ───────────────────────────────────────────────────────────────────────

public record GetSettingsQuery : IRequest<SettingsDto>;

public class GetSettingsHandler(IAppDbContext db) : IRequestHandler<GetSettingsQuery, SettingsDto>
{
    public async Task<SettingsDto> Handle(GetSettingsQuery _, CancellationToken ct)
    {
        var templatesTask  = db.ChecklistTemplates
            .OrderBy(t => t.ItemType).ThenBy(t => t.SortOrder)
            .ToListAsync(ct);

        var platformsTask  = db.Platforms
            .OrderBy(p => p.SortOrder).ThenBy(p => p.Name)
            .ToListAsync(ct);

        await Task.WhenAll(templatesTask, platformsTask);

        var templates = templatesTask.Result
            .Select(t => new ChecklistTemplateDto(t.Id, t.ItemType.ToString(), t.Label, t.SortOrder))
            .ToList();

        var platforms = platformsTask.Result
            .Select(p => new PlatformDto(p.Id, p.Name, p.SortOrder))
            .ToList();

        return new SettingsDto(templates, platforms);
    }
}

// ── SAVE (batch) ──────────────────────────────────────────────────────────────

public class SaveSettingsHandler(IAppDbContext db) : IRequestHandler<SaveSettingsCommand>
{
    public async Task Handle(SaveSettingsCommand cmd, CancellationToken ct)
    {
        await SaveTemplatesAsync(cmd.Templates, ct);
        await SavePlatformsAsync(cmd.Platforms, ct);
    }

    private async Task SaveTemplatesAsync(List<TemplateSaveDto> incoming, CancellationToken ct)
    {
        var existing = await db.ChecklistTemplates.ToListAsync(ct);
        var incomingIds = incoming.Where(t => t.Id.HasValue).Select(t => t.Id!.Value).ToHashSet();

        // Delete removed
        var toDelete = existing.Where(e => !incomingIds.Contains(e.Id)).ToList();
        foreach (var t in toDelete) db.ChecklistTemplates.Remove(t);

        foreach (var dto in incoming)
        {
            var itemType = dto.ItemType == "Game"
                ? GameArchive.Domain.ItemType.Game
                : GameArchive.Domain.ItemType.Console;

            if (dto.Id.HasValue)
            {
                var entity = existing.FirstOrDefault(e => e.Id == dto.Id.Value);
                if (entity is null) continue;
                entity.Label     = dto.Label;
                entity.SortOrder = dto.SortOrder;
                entity.ItemType  = itemType;
            }
            else
            {
                db.ChecklistTemplates.Add(new GameArchive.Domain.Entities.ChecklistTemplate
                {
                    ItemType  = itemType,
                    Label     = dto.Label,
                    SortOrder = dto.SortOrder
                });
            }
        }

        await db.SaveChangesAsync(ct);
    }

    private async Task SavePlatformsAsync(List<PlatformSaveDto> incoming, CancellationToken ct)
    {
        var existing = await db.Platforms.ToListAsync(ct);
        var incomingNames = incoming.Select(p => p.Name.Trim().ToLowerInvariant()).ToHashSet();

        // Delete removed
        var toDelete = existing
            .Where(e => !incomingNames.Contains(e.Name.Trim().ToLowerInvariant()))
            .ToList();
        foreach (var p in toDelete) db.Platforms.Remove(p);

        for (int i = 0; i < incoming.Count; i++)
        {
            var dto   = incoming[i];
            var name  = dto.Name.Trim();
            var order = dto.SortOrder;

            var entity = existing.FirstOrDefault(e =>
                e.Name.Trim().ToLowerInvariant() == name.ToLowerInvariant());

            if (entity is not null)
            {
                entity.Name      = name;
                entity.SortOrder = order;
            }
            else
            {
                db.Platforms.Add(new GameArchive.Domain.Entities.Platform
                {
                    Name      = name,
                    SortOrder = order
                });
            }
        }

        await db.SaveChangesAsync(ct);
    }
}
