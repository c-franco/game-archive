using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace GameArchive.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPlatforms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Platforms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Platforms", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Platforms",
                columns: new[] { "Id", "Name", "SortOrder" },
                values: new object[,]
                {
                    { new Guid("22222222-0000-0000-0000-000000000001"), "NES", 1 },
                    { new Guid("22222222-0000-0000-0000-000000000002"), "SNES", 2 },
                    { new Guid("22222222-0000-0000-0000-000000000003"), "Nintendo 64", 3 },
                    { new Guid("22222222-0000-0000-0000-000000000004"), "GameCube", 4 },
                    { new Guid("22222222-0000-0000-0000-000000000005"), "Wii", 5 },
                    { new Guid("22222222-0000-0000-0000-000000000006"), "Wii U", 6 },
                    { new Guid("22222222-0000-0000-0000-000000000007"), "Nintendo Switch", 7 },
                    { new Guid("22222222-0000-0000-0000-000000000008"), "Game Boy", 8 },
                    { new Guid("22222222-0000-0000-0000-000000000009"), "Game Boy Color", 9 },
                    { new Guid("22222222-0000-0000-0000-000000000010"), "Game Boy Advance", 10 },
                    { new Guid("22222222-0000-0000-0000-000000000011"), "Nintendo DS", 11 },
                    { new Guid("22222222-0000-0000-0000-000000000012"), "Nintendo 3DS", 12 },
                    { new Guid("22222222-0000-0000-0000-000000000013"), "PlayStation", 13 },
                    { new Guid("22222222-0000-0000-0000-000000000014"), "PlayStation 2", 14 },
                    { new Guid("22222222-0000-0000-0000-000000000015"), "PlayStation 3", 15 },
                    { new Guid("22222222-0000-0000-0000-000000000016"), "PlayStation 4", 16 },
                    { new Guid("22222222-0000-0000-0000-000000000017"), "PlayStation 5", 17 },
                    { new Guid("22222222-0000-0000-0000-000000000018"), "PSP", 18 },
                    { new Guid("22222222-0000-0000-0000-000000000019"), "PS Vita", 19 },
                    { new Guid("22222222-0000-0000-0000-000000000020"), "Xbox", 20 },
                    { new Guid("22222222-0000-0000-0000-000000000021"), "Xbox 360", 21 },
                    { new Guid("22222222-0000-0000-0000-000000000022"), "Xbox One", 22 },
                    { new Guid("22222222-0000-0000-0000-000000000023"), "Xbox Series X/S", 23 },
                    { new Guid("22222222-0000-0000-0000-000000000024"), "Sega Mega Drive", 24 },
                    { new Guid("22222222-0000-0000-0000-000000000025"), "Sega Saturn", 25 },
                    { new Guid("22222222-0000-0000-0000-000000000026"), "Sega Dreamcast", 26 },
                    { new Guid("22222222-0000-0000-0000-000000000027"), "PC", 27 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Platforms");
        }
    }
}
