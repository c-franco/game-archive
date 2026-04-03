using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameArchive.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProductUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProductUrl",
                table: "Items",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProductUrl",
                table: "Items");
        }
    }
}
