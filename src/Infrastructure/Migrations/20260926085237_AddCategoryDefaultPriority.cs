using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZEGU.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryDefaultPriority : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DefaultPriority",
                table: "MaintenanceCategories",
                type: "integer",
                nullable: false,
                defaultValue: 2); // RequestPriority.Normal

            // Backfill sensible defaults for the categories this project seeds out of the box.
            // Anything not matched here (custom categories an admin added) keeps the column default (Normal).
            migrationBuilder.Sql(@"
                UPDATE ""MaintenanceCategories"" SET ""DefaultPriority"" = 3 WHERE ""CategoryName"" IN ('Electrical', 'Sanitation', 'Generator'); -- High
                UPDATE ""MaintenanceCategories"" SET ""DefaultPriority"" = 1 WHERE ""CategoryName"" IN ('Carpentry', 'Painting', 'Furniture', 'Grounds'); -- Low
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultPriority",
                table: "MaintenanceCategories");
        }
    }
}
