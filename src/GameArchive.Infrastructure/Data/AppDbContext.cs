using GameArchive.Application.Common;
using GameArchive.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GameArchive.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options), IAppDbContext
{
    public DbSet<CollectionItem>    Items               => Set<CollectionItem>();
    public DbSet<ChecklistTemplate> ChecklistTemplates  => Set<ChecklistTemplate>();
    public DbSet<ChecklistEntry>    ChecklistEntries     => Set<ChecklistEntry>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<CollectionItem>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.PurchasePrice).HasPrecision(10, 2);
            e.Property(x => x.EstimatedValue).HasPrecision(10, 2);
            e.HasMany(x => x.ChecklistEntries)
             .WithOne(c => c.CollectionItem)
             .HasForeignKey(c => c.CollectionItemId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<ChecklistTemplate>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Label).IsRequired().HasMaxLength(100);
        });

        b.Entity<ChecklistEntry>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Label).IsRequired().HasMaxLength(100);
        });

        // Seed default checklist templates
        b.Entity<ChecklistTemplate>().HasData(
            new ChecklistTemplate { Id = Guid.Parse("11111111-0000-0000-0000-000000000001"), ItemType = Domain.ItemType.Console, Label = "Box",             SortOrder = 1 },
            new ChecklistTemplate { Id = Guid.Parse("11111111-0000-0000-0000-000000000002"), ItemType = Domain.ItemType.Console, Label = "Controller",      SortOrder = 2 },
            new ChecklistTemplate { Id = Guid.Parse("11111111-0000-0000-0000-000000000003"), ItemType = Domain.ItemType.Console, Label = "Cables",          SortOrder = 3 },
            new ChecklistTemplate { Id = Guid.Parse("11111111-0000-0000-0000-000000000004"), ItemType = Domain.ItemType.Console, Label = "Manual",          SortOrder = 4 },
            new ChecklistTemplate { Id = Guid.Parse("11111111-0000-0000-0000-000000000005"), ItemType = Domain.ItemType.Game,    Label = "Box",             SortOrder = 1 },
            new ChecklistTemplate { Id = Guid.Parse("11111111-0000-0000-0000-000000000006"), ItemType = Domain.ItemType.Game,    Label = "Manual",          SortOrder = 2 },
            new ChecklistTemplate { Id = Guid.Parse("11111111-0000-0000-0000-000000000007"), ItemType = Domain.ItemType.Game,    Label = "Cartridge/Disc",  SortOrder = 3 }
        );
    }
}
