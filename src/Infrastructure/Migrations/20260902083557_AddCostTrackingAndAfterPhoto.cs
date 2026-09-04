using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZEGU.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCostTrackingAndAfterPhoto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "LaborRate",
                table: "WorkLogs",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AfterPhotoPath",
                table: "MaintenanceRequests",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LaborCost",
                table: "MaintenanceRequests",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MaterialCost",
                table: "MaintenanceRequests",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OtherCost",
                table: "MaintenanceRequests",
                type: "numeric",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LaborRate",
                table: "WorkLogs");

            migrationBuilder.DropColumn(
                name: "AfterPhotoPath",
                table: "MaintenanceRequests");

            migrationBuilder.DropColumn(
                name: "LaborCost",
                table: "MaintenanceRequests");

            migrationBuilder.DropColumn(
                name: "MaterialCost",
                table: "MaintenanceRequests");

            migrationBuilder.DropColumn(
                name: "OtherCost",
                table: "MaintenanceRequests");
        }
    }
}
