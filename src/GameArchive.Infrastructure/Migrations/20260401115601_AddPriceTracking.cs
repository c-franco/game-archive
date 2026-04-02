using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameArchive.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPriceTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PriceLastFetchedAt",
                table: "Items",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PriceSource",
                table: "Items",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PriceLastFetchedAt",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "PriceSource",
                table: "Items");
        }
    }
}
