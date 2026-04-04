using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameArchive.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveConsoleManual : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "ChecklistTemplates",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0000-0000-0000-000000000001"),
                columns: new[] { "Label", "SortOrder" },
                values: new object[] { "Consola", 0 });

            migrationBuilder.UpdateData(
                table: "ChecklistTemplates",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0000-0000-0000-000000000002"),
                columns: new[] { "Label", "SortOrder" },
                values: new object[] { "Caja", 1 });

            migrationBuilder.UpdateData(
                table: "ChecklistTemplates",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0000-0000-0000-000000000003"),
                columns: new[] { "Label", "SortOrder" },
                values: new object[] { "Mando", 2 });

            migrationBuilder.InsertData(
                table: "ChecklistTemplates",
                columns: new[] { "Id", "ItemType", "Label", "SortOrder" },
                values: new object[] { new Guid("11111111-0000-0000-0000-000000000004"), 1, "Cables", 3 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ChecklistTemplates",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0000-0000-0000-000000000004"));

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
    }
}
