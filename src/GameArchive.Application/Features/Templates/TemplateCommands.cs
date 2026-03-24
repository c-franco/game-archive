using GameArchive.Application.Common;
using GameArchive.Application.DTOs;
using GameArchive.Domain;
using GameArchive.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GameArchive.Application.Features.Templates;

// ── GET ALL ──────────────────────────────────────────────────────────────────

public record GetTemplatesQuery : IRequest<List<ChecklistTemplateDto>>;

public class GetTemplatesHandler(IAppDbContext db) : IRequestHandler<GetTemplatesQuery, List<ChecklistTemplateDto>>
{
    public async Task<List<ChecklistTemplateDto>> Handle(GetTemplatesQuery _, CancellationToken ct)
    {
        var templates = await db.ChecklistTemplates
            .OrderBy(t => t.ItemType).ThenBy(t => t.SortOrder)
            .ToListAsync(ct);

        return templates.Select(t => new ChecklistTemplateDto(t.Id, t.ItemType.ToString(), t.Label, t.SortOrder)).ToList();
    }
}

// ── CREATE ───────────────────────────────────────────────────────────────────

public record CreateTemplateCommand(ItemType ItemType, string Label, int SortOrder) : IRequest<ChecklistTemplateDto>;

public class CreateTemplateHandler(IAppDbContext db) : IRequestHandler<CreateTemplateCommand, ChecklistTemplateDto>
{
    public async Task<ChecklistTemplateDto> Handle(CreateTemplateCommand cmd, CancellationToken ct)
    {
        var t = new ChecklistTemplate { ItemType = cmd.ItemType, Label = cmd.Label, SortOrder = cmd.SortOrder };
        db.ChecklistTemplates.Add(t);
        await db.SaveChangesAsync(ct);
        return new ChecklistTemplateDto(t.Id, t.ItemType.ToString(), t.Label, t.SortOrder);
    }
}

// ── UPDATE ───────────────────────────────────────────────────────────────────

public record UpdateTemplateCommand(Guid Id, string Label, int SortOrder) : IRequest;

public class UpdateTemplateHandler(IAppDbContext db) : IRequestHandler<UpdateTemplateCommand>
{
    public async Task Handle(UpdateTemplateCommand cmd, CancellationToken ct)
    {
        var t = await db.ChecklistTemplates.FindAsync([cmd.Id], ct)
            ?? throw new KeyNotFoundException();
        t.Label     = cmd.Label;
        t.SortOrder = cmd.SortOrder;
        await db.SaveChangesAsync(ct);
    }
}

// ── DELETE ───────────────────────────────────────────────────────────────────

public record DeleteTemplateCommand(Guid Id) : IRequest;

public class DeleteTemplateHandler(IAppDbContext db) : IRequestHandler<DeleteTemplateCommand>
{
    public async Task Handle(DeleteTemplateCommand cmd, CancellationToken ct)
    {
        var t = await db.ChecklistTemplates.FindAsync([cmd.Id], ct)
            ?? throw new KeyNotFoundException();
        db.ChecklistTemplates.Remove(t);
        await db.SaveChangesAsync(ct);
    }
}
