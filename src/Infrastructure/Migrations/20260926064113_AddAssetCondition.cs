using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZEGU.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAssetCondition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Condition",
                table: "Assets",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Condition",
                table: "Assets");
        }
    }
}
