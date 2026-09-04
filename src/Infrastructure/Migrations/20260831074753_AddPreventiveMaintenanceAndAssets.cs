using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ZEGU.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPreventiveMaintenanceAndAssets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AssetId",
                table: "MaintenanceRequests",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AssetId1",
                table: "MaintenanceRequests",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Assets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AssetName = table.Column<string>(type: "text", nullable: false),
                    AssetCode = table.Column<string>(type: "text", nullable: true),
                    SerialNumber = table.Column<string>(type: "text", nullable: true),
                    CategoryId = table.Column<int>(type: "integer", nullable: false),
                    LocationId = table.Column<int>(type: "integer", nullable: false),
                    PurchaseDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    WarrantyExpiry = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Manufacturer = table.Column<string>(type: "text", nullable: true),
                    Model = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: true),
                    QrCodePath = table.Column<string>(type: "text", nullable: true),
                    BarcodePath = table.Column<string>(type: "text", nullable: true),
                    MaintenanceCategoryId = table.Column<int>(type: "integer", nullable: true),
                    RoomId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Assets_MaintenanceCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "MaintenanceCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Assets_MaintenanceCategories_MaintenanceCategoryId",
                        column: x => x.MaintenanceCategoryId,
                        principalTable: "MaintenanceCategories",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Assets_Rooms_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Assets_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PreventiveMaintenanceSchedules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ScheduleName = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CategoryId = table.Column<int>(type: "integer", nullable: false),
                    LocationId = table.Column<int>(type: "integer", nullable: false),
                    TechnicianId = table.Column<int>(type: "integer", nullable: true),
                    Frequency = table.Column<string>(type: "text", nullable: false),
                    FrequencyDays = table.Column<int>(type: "integer", nullable: false),
                    LastPerformed = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NextDue = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    MaintenanceCategoryId = table.Column<int>(type: "integer", nullable: true),
                    RoomId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreventiveMaintenanceSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PreventiveMaintenanceSchedules_MaintenanceCategories_Catego~",
                        column: x => x.CategoryId,
                        principalTable: "MaintenanceCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PreventiveMaintenanceSchedules_MaintenanceCategories_Mainte~",
                        column: x => x.MaintenanceCategoryId,
                        principalTable: "MaintenanceCategories",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PreventiveMaintenanceSchedules_Rooms_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PreventiveMaintenanceSchedules_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PreventiveMaintenanceSchedules_Technicians_TechnicianId",
                        column: x => x.TechnicianId,
                        principalTable: "Technicians",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PreventiveMaintenanceRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ScheduleId = table.Column<int>(type: "integer", nullable: false),
                    TechnicianId = table.Column<int>(type: "integer", nullable: true),
                    PerformedById = table.Column<string>(type: "text", nullable: true),
                    WorkPerformed = table.Column<string>(type: "text", nullable: false),
                    PerformedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NextDue = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreventiveMaintenanceRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PreventiveMaintenanceRecords_AspNetUsers_PerformedById",
                        column: x => x.PerformedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PreventiveMaintenanceRecords_PreventiveMaintenanceSchedules~",
                        column: x => x.ScheduleId,
                        principalTable: "PreventiveMaintenanceSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PreventiveMaintenanceRecords_Technicians_TechnicianId",
                        column: x => x.TechnicianId,
                        principalTable: "Technicians",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceRequests_AssetId",
                table: "MaintenanceRequests",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceRequests_AssetId1",
                table: "MaintenanceRequests",
                column: "AssetId1");

            migrationBuilder.CreateIndex(
                name: "IX_Assets_AssetCode",
                table: "Assets",
                column: "AssetCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Assets_CategoryId",
                table: "Assets",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Assets_LocationId",
                table: "Assets",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Assets_MaintenanceCategoryId",
                table: "Assets",
                column: "MaintenanceCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Assets_RoomId",
                table: "Assets",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_PreventiveMaintenanceRecords_PerformedById",
                table: "PreventiveMaintenanceRecords",
                column: "PerformedById");

            migrationBuilder.CreateIndex(
                name: "IX_PreventiveMaintenanceRecords_ScheduleId",
                table: "PreventiveMaintenanceRecords",
                column: "ScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_PreventiveMaintenanceRecords_TechnicianId",
                table: "PreventiveMaintenanceRecords",
                column: "TechnicianId");

            migrationBuilder.CreateIndex(
                name: "IX_PreventiveMaintenanceSchedules_CategoryId",
                table: "PreventiveMaintenanceSchedules",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_PreventiveMaintenanceSchedules_LocationId",
                table: "PreventiveMaintenanceSchedules",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_PreventiveMaintenanceSchedules_MaintenanceCategoryId",
                table: "PreventiveMaintenanceSchedules",
                column: "MaintenanceCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_PreventiveMaintenanceSchedules_RoomId",
                table: "PreventiveMaintenanceSchedules",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_PreventiveMaintenanceSchedules_TechnicianId",
                table: "PreventiveMaintenanceSchedules",
                column: "TechnicianId");

            migrationBuilder.AddForeignKey(
                name: "FK_MaintenanceRequests_Assets_AssetId",
                table: "MaintenanceRequests",
                column: "AssetId",
                principalTable: "Assets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MaintenanceRequests_Assets_AssetId1",
                table: "MaintenanceRequests",
                column: "AssetId1",
                principalTable: "Assets",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaintenanceRequests_Assets_AssetId",
                table: "MaintenanceRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_MaintenanceRequests_Assets_AssetId1",
                table: "MaintenanceRequests");

            migrationBuilder.DropTable(
                name: "Assets");

            migrationBuilder.DropTable(
                name: "PreventiveMaintenanceRecords");

            migrationBuilder.DropTable(
                name: "PreventiveMaintenanceSchedules");

            migrationBuilder.DropIndex(
                name: "IX_MaintenanceRequests_AssetId",
                table: "MaintenanceRequests");

            migrationBuilder.DropIndex(
                name: "IX_MaintenanceRequests_AssetId1",
                table: "MaintenanceRequests");

            migrationBuilder.DropColumn(
                name: "AssetId",
                table: "MaintenanceRequests");

            migrationBuilder.DropColumn(
                name: "AssetId1",
                table: "MaintenanceRequests");
        }
    }
}
