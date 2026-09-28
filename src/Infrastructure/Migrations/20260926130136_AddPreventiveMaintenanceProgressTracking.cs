using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZEGU.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPreventiveMaintenanceProgressTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AssetId",
                table: "PreventiveMaintenanceSchedules",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartedAt",
                table: "PreventiveMaintenanceSchedules",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StartedById",
                table: "PreventiveMaintenanceSchedules",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "PreventiveMaintenanceSchedules",
                type: "integer",
                nullable: false,
                defaultValue: 1); // PreventiveMaintenanceStatus.Scheduled

            migrationBuilder.AddColumn<DateTime>(
                name: "StartedAt",
                table: "PreventiveMaintenanceRecords",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PreventiveMaintenanceSchedules_AssetId",
                table: "PreventiveMaintenanceSchedules",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_PreventiveMaintenanceSchedules_StartedById",
                table: "PreventiveMaintenanceSchedules",
                column: "StartedById");

            migrationBuilder.AddForeignKey(
                name: "FK_PreventiveMaintenanceSchedules_AspNetUsers_StartedById",
                table: "PreventiveMaintenanceSchedules",
                column: "StartedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_PreventiveMaintenanceSchedules_Assets_AssetId",
                table: "PreventiveMaintenanceSchedules",
                column: "AssetId",
                principalTable: "Assets",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PreventiveMaintenanceSchedules_AspNetUsers_StartedById",
                table: "PreventiveMaintenanceSchedules");

            migrationBuilder.DropForeignKey(
                name: "FK_PreventiveMaintenanceSchedules_Assets_AssetId",
                table: "PreventiveMaintenanceSchedules");

            migrationBuilder.DropIndex(
                name: "IX_PreventiveMaintenanceSchedules_AssetId",
                table: "PreventiveMaintenanceSchedules");

            migrationBuilder.DropIndex(
                name: "IX_PreventiveMaintenanceSchedules_StartedById",
                table: "PreventiveMaintenanceSchedules");

            migrationBuilder.DropColumn(
                name: "AssetId",
                table: "PreventiveMaintenanceSchedules");

            migrationBuilder.DropColumn(
                name: "StartedAt",
                table: "PreventiveMaintenanceSchedules");

            migrationBuilder.DropColumn(
                name: "StartedById",
                table: "PreventiveMaintenanceSchedules");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "PreventiveMaintenanceSchedules");

            migrationBuilder.DropColumn(
                name: "StartedAt",
                table: "PreventiveMaintenanceRecords");
        }
    }
}
