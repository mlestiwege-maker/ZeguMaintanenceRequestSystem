using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZEGU.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPreventiveMaintenanceReminderTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaintenanceRequests_Assets_AssetId1",
                table: "MaintenanceRequests");

            migrationBuilder.DropIndex(
                name: "IX_MaintenanceRequests_AssetId1",
                table: "MaintenanceRequests");

            migrationBuilder.DropColumn(
                name: "AssetId1",
                table: "MaintenanceRequests");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastReminderSentAt",
                table: "PreventiveMaintenanceSchedules",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastReminderSentAt",
                table: "PreventiveMaintenanceSchedules");

            migrationBuilder.AddColumn<int>(
                name: "AssetId1",
                table: "MaintenanceRequests",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceRequests_AssetId1",
                table: "MaintenanceRequests",
                column: "AssetId1");

            migrationBuilder.AddForeignKey(
                name: "FK_MaintenanceRequests_Assets_AssetId1",
                table: "MaintenanceRequests",
                column: "AssetId1",
                principalTable: "Assets",
                principalColumn: "Id");
        }
    }
}
