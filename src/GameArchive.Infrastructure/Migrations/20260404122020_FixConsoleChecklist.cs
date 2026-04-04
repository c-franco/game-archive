using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameArchive.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixConsoleChecklist : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Delete old "Manual" for console (will be replaced with "Cables")
            migrationBuilder.DeleteData(
                table: "ChecklistTemplates",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0000-0000-0000-000000000004"));

            // Add new "Consola" entry for console (using a new ID)
            migrationBuilder.InsertData(
                table: "ChecklistTemplates",
                columns: new[] { "Id", "ItemType", "Label", "SortOrder" },
                values: new object[] { new Guid("11111111-0000-0000-0000-000000000008"), 1, "Consola", 0 });

            migrationBuilder.UpdateData(
                table: "ChecklistTemplates",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0000-0000-0000-000000000001"),
                columns: new[] { "Label", "SortOrder" },
                values: new object[] { "Caja", 1 });

            migrationBuilder.UpdateData(
                table: "ChecklistTemplates",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0000-0000-0000-000000000002"),
                columns: new[] { "Label", "SortOrder" },
                values: new object[] { "Mando", 2 });

            migrationBuilder.UpdateData(
                table: "ChecklistTemplates",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0000-0000-0000-000000000003"),
                columns: new[] { "Label", "SortOrder" },
                values: new object[] { "Cables", 3 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert updates
            migrationBuilder.UpdateData(
                table: "ChecklistTemplates",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0000-0000-0000-000000000001"),
                columns: new[] { "Label", "SortOrder" },
                values: new object[] { "Caja", 1 });

            migrationBuilder.UpdateData(
                table: "ChecklistTemplates",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0000-0000-0000-000000000002"),
                columns: new[] { "Label", "SortOrder" },
                values: new object[] { "Mando", 2 });

            migrationBuilder.UpdateData(
                table: "ChecklistTemplates",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0000-0000-0000-000000000003"),
                columns: new[] { "Label", "SortOrder" },
                values: new object[] { "Cables", 3 });

            // Delete the new "Consola" entry
            migrationBuilder.DeleteData(
                table: "ChecklistTemplates",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0000-0000-0000-000000000008"));

            // Re-insert the old "Manual" entry
            migrationBuilder.InsertData(
                table: "ChecklistTemplates",
                columns: new[] { "Id", "ItemType", "Label", "SortOrder" },
                values: new object[] { new Guid("11111111-0000-0000-0000-000000000004"), 1, "Manual", 4 });
        }
    }
}
