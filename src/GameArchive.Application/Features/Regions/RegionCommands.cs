using GameArchive.Application.Common;
using GameArchive.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GameArchive.Application.Features.Regions;

public record RegionDto(Guid Id, string Name, int SortOrder);

public record GetRegionsQuery : IRequest<List<RegionDto>>;

public class GetRegionsHandler(IAppDbContext db) : IRequestHandler<GetRegionsQuery, List<RegionDto>>
{
    public async Task<List<RegionDto>> Handle(GetRegionsQuery _, CancellationToken ct)
    {
        var list = await db.Regions.OrderBy(r => r.SortOrder).ThenBy(r => r.Name).ToListAsync(ct);
        return list.Select(r => new RegionDto(r.Id, r.Name, r.SortOrder)).ToList();
    }
}

public record CreateRegionCommand(string Name, int SortOrder) : IRequest<RegionDto>;

public class CreateRegionHandler(IAppDbContext db) : IRequestHandler<CreateRegionCommand, RegionDto>
{
    public async Task<RegionDto> Handle(CreateRegionCommand cmd, CancellationToken ct)
    {
        var r = new Region { Name = cmd.Name.Trim(), SortOrder = cmd.SortOrder };
        db.Regions.Add(r);
        await db.SaveChangesAsync(ct);
        return new RegionDto(r.Id, r.Name, r.SortOrder);
    }
}

public record UpdateRegionCommand(Guid Id, string Name, int SortOrder) : IRequest;

public class UpdateRegionHandler(IAppDbContext db) : IRequestHandler<UpdateRegionCommand>
{
    public async Task Handle(UpdateRegionCommand cmd, CancellationToken ct)
    {
        var r = await db.Regions.FindAsync([cmd.Id], ct)
            ?? throw new KeyNotFoundException();
        r.Name = cmd.Name.Trim();
        r.SortOrder = cmd.SortOrder;
        await db.SaveChangesAsync(ct);
    }
}

public record DeleteRegionCommand(Guid Id) : IRequest;

public class DeleteRegionHandler(IAppDbContext db) : IRequestHandler<DeleteRegionCommand>
{
    public async Task Handle(DeleteRegionCommand cmd, CancellationToken ct)
    {
        var r = await db.Regions.FindAsync([cmd.Id], ct)
            ?? throw new KeyNotFoundException();
        db.Regions.Remove(r);
        await db.SaveChangesAsync(ct);
    }
}
