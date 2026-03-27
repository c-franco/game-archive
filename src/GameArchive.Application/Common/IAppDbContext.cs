using GameArchive.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GameArchive.Application.Common;

public interface IAppDbContext
{
    DbSet<CollectionItem>    Items               { get; }
    DbSet<ChecklistTemplate> ChecklistTemplates  { get; }
    DbSet<ChecklistEntry>    ChecklistEntries    { get; }
    DbSet<Platform>          Platforms           { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
