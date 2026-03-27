using GameArchive.Application.Common;
using GameArchive.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GameArchive.Application.Features.Platforms;

// ── DTOs ─────────────────────────────────────────────────────────────────────

public record PlatformDto(Guid Id, string Name, int SortOrder);

// ── GET ALL ───────────────────────────────────────────────────────────────────

public record GetPlatformsQuery : IRequest<List<PlatformDto>>;

public class GetPlatformsHandler(IAppDbContext db) : IRequestHandler<GetPlatformsQuery, List<PlatformDto>>
{
    public async Task<List<PlatformDto>> Handle(GetPlatformsQuery _, CancellationToken ct)
    {
        var list = await db.Platforms.OrderBy(p => p.SortOrder).ThenBy(p => p.Name).ToListAsync(ct);
        return list.Select(p => new PlatformDto(p.Id, p.Name, p.SortOrder)).ToList();
    }
}

// ── CREATE ────────────────────────────────────────────────────────────────────

public record CreatePlatformCommand(string Name, int SortOrder) : IRequest<PlatformDto>;

public class CreatePlatformHandler(IAppDbContext db) : IRequestHandler<CreatePlatformCommand, PlatformDto>
{
    public async Task<PlatformDto> Handle(CreatePlatformCommand cmd, CancellationToken ct)
    {
        var p = new Platform { Name = cmd.Name.Trim(), SortOrder = cmd.SortOrder };
        db.Platforms.Add(p);
        await db.SaveChangesAsync(ct);
        return new PlatformDto(p.Id, p.Name, p.SortOrder);
    }
}

// ── UPDATE ────────────────────────────────────────────────────────────────────

public record UpdatePlatformCommand(Guid Id, string Name, int SortOrder) : IRequest;

public class UpdatePlatformHandler(IAppDbContext db) : IRequestHandler<UpdatePlatformCommand>
{
    public async Task Handle(UpdatePlatformCommand cmd, CancellationToken ct)
    {
        var p = await db.Platforms.FindAsync([cmd.Id], ct)
            ?? throw new KeyNotFoundException();
        p.Name      = cmd.Name.Trim();
        p.SortOrder = cmd.SortOrder;
        await db.SaveChangesAsync(ct);
    }
}

// ── DELETE ────────────────────────────────────────────────────────────────────

public record DeletePlatformCommand(Guid Id) : IRequest;

public class DeletePlatformHandler(IAppDbContext db) : IRequestHandler<DeletePlatformCommand>
{
    public async Task Handle(DeletePlatformCommand cmd, CancellationToken ct)
    {
        var p = await db.Platforms.FindAsync([cmd.Id], ct)
            ?? throw new KeyNotFoundException();
        db.Platforms.Remove(p);
        await db.SaveChangesAsync(ct);
    }
}
