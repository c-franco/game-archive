using GameArchive.Application.Common;
using GameArchive.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GameArchive.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options), IAppDbContext
{
    public DbSet<CollectionItem>    Items               => Set<CollectionItem>();
    public DbSet<ChecklistTemplate> ChecklistTemplates  => Set<ChecklistTemplate>();
    public DbSet<ChecklistEntry>    ChecklistEntries    => Set<ChecklistEntry>();
    public DbSet<Platform>          Platforms           => Set<Platform>();
    public DbSet<Region>            Regions             => Set<Region>();

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

        b.Entity<Platform>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(100);
        });

        b.Entity<Region>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(100);
        });

        // Seed default checklist templates
        b.Entity<ChecklistTemplate>().HasData(
            new ChecklistTemplate { Id = Guid.Parse("11111111-0000-0000-0000-000000000001"), ItemType = Domain.ItemType.Console, Label = "Box",            SortOrder = 1 },
            new ChecklistTemplate { Id = Guid.Parse("11111111-0000-0000-0000-000000000002"), ItemType = Domain.ItemType.Console, Label = "Controller",     SortOrder = 2 },
            new ChecklistTemplate { Id = Guid.Parse("11111111-0000-0000-0000-000000000003"), ItemType = Domain.ItemType.Console, Label = "Cables",         SortOrder = 3 },
            new ChecklistTemplate { Id = Guid.Parse("11111111-0000-0000-0000-000000000004"), ItemType = Domain.ItemType.Console, Label = "Manual",         SortOrder = 4 },
            new ChecklistTemplate { Id = Guid.Parse("11111111-0000-0000-0000-000000000005"), ItemType = Domain.ItemType.Game,    Label = "Box",            SortOrder = 1 },
            new ChecklistTemplate { Id = Guid.Parse("11111111-0000-0000-0000-000000000006"), ItemType = Domain.ItemType.Game,    Label = "Manual",         SortOrder = 2 },
            new ChecklistTemplate { Id = Guid.Parse("11111111-0000-0000-0000-000000000007"), ItemType = Domain.ItemType.Game,    Label = "Cartridge/Disc", SortOrder = 3 }
        );

        // Seed default platforms
        b.Entity<Platform>().HasData(
            new Platform { Id = Guid.Parse("22222222-0000-0000-0000-000000000001"), Name = "NES",              SortOrder = 1  },
            new Platform { Id = Guid.Parse("22222222-0000-0000-0000-000000000002"), Name = "SNES",             SortOrder = 2  },
            new Platform { Id = Guid.Parse("22222222-0000-0000-0000-000000000003"), Name = "Nintendo 64",      SortOrder = 3  },
            new Platform { Id = Guid.Parse("22222222-0000-0000-0000-000000000004"), Name = "GameCube",         SortOrder = 4  },
            new Platform { Id = Guid.Parse("22222222-0000-0000-0000-000000000005"), Name = "Wii",              SortOrder = 5  },
            new Platform { Id = Guid.Parse("22222222-0000-0000-0000-000000000006"), Name = "Wii U",            SortOrder = 6  },
            new Platform { Id = Guid.Parse("22222222-0000-0000-0000-000000000007"), Name = "Nintendo Switch",  SortOrder = 7  },
            new Platform { Id = Guid.Parse("22222222-0000-0000-0000-000000000008"), Name = "Game Boy",         SortOrder = 8  },
            new Platform { Id = Guid.Parse("22222222-0000-0000-0000-000000000009"), Name = "Game Boy Color",   SortOrder = 9  },
            new Platform { Id = Guid.Parse("22222222-0000-0000-0000-000000000010"), Name = "Game Boy Advance", SortOrder = 10 },
            new Platform { Id = Guid.Parse("22222222-0000-0000-0000-000000000011"), Name = "Nintendo DS",      SortOrder = 11 },
            new Platform { Id = Guid.Parse("22222222-0000-0000-0000-000000000012"), Name = "Nintendo 3DS",     SortOrder = 12 },
            new Platform { Id = Guid.Parse("22222222-0000-0000-0000-000000000013"), Name = "PlayStation",      SortOrder = 13 },
            new Platform { Id = Guid.Parse("22222222-0000-0000-0000-000000000014"), Name = "PlayStation 2",    SortOrder = 14 },
            new Platform { Id = Guid.Parse("22222222-0000-0000-0000-000000000015"), Name = "PlayStation 3",    SortOrder = 15 },
            new Platform { Id = Guid.Parse("22222222-0000-0000-0000-000000000016"), Name = "PlayStation 4",    SortOrder = 16 },
            new Platform { Id = Guid.Parse("22222222-0000-0000-0000-000000000017"), Name = "PlayStation 5",    SortOrder = 17 },
            new Platform { Id = Guid.Parse("22222222-0000-0000-0000-000000000018"), Name = "PSP",              SortOrder = 18 },
            new Platform { Id = Guid.Parse("22222222-0000-0000-0000-000000000019"), Name = "PS Vita",          SortOrder = 19 },
            new Platform { Id = Guid.Parse("22222222-0000-0000-0000-000000000020"), Name = "Xbox",             SortOrder = 20 },
            new Platform { Id = Guid.Parse("22222222-0000-0000-0000-000000000021"), Name = "Xbox 360",         SortOrder = 21 },
            new Platform { Id = Guid.Parse("22222222-0000-0000-0000-000000000022"), Name = "Xbox One",         SortOrder = 22 },
            new Platform { Id = Guid.Parse("22222222-0000-0000-0000-000000000023"), Name = "Xbox Series X/S",  SortOrder = 23 },
            new Platform { Id = Guid.Parse("22222222-0000-0000-0000-000000000024"), Name = "Sega Mega Drive",  SortOrder = 24 },
            new Platform { Id = Guid.Parse("22222222-0000-0000-0000-000000000025"), Name = "Sega Saturn",      SortOrder = 25 },
            new Platform { Id = Guid.Parse("22222222-0000-0000-0000-000000000026"), Name = "Sega Dreamcast",   SortOrder = 26 },
            new Platform { Id = Guid.Parse("22222222-0000-0000-0000-000000000027"), Name = "PC",               SortOrder = 27 }
        );

        // Seed default regions
        b.Entity<Region>().HasData(
            new Region { Id = Guid.Parse("33333333-0000-0000-0000-000000000001"), Name = "PAL",     SortOrder = 1 },
            new Region { Id = Guid.Parse("33333333-0000-0000-0000-000000000002"), Name = "NTSC-U",  SortOrder = 2 },
            new Region { Id = Guid.Parse("33333333-0000-0000-0000-000000000003"), Name = "NTSC-J",  SortOrder = 3 },
            new Region { Id = Guid.Parse("33333333-0000-0000-0000-000000000004"), Name = "NTSC",    SortOrder = 4 },
            new Region { Id = Guid.Parse("33333333-0000-0000-0000-000000000005"), Name = "Unknown", SortOrder = 5 }
        );
    }
}
