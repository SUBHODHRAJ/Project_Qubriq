using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace Qubriq.API.Migrations
{
    /// <inheritdoc />
    public partial class AddLoadOptimizationPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CargoHeightCm",
                table: "Vehicles",
                type: "decimal(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CargoLengthCm",
                table: "Vehicles",
                type: "decimal(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CargoWidthCm",
                table: "Vehicles",
                type: "decimal(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OptimizationRuns",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    ShipmentId = table.Column<long>(type: "bigint", nullable: false),
                    VehicleId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    Algorithm = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    IsFeasible = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    VehicleLengthCm = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    VehicleWidthCm = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    VehicleHeightCm = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    VehicleCapacityKg = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    TotalPackages = table.Column<int>(type: "int", nullable: false),
                    PlacedPackages = table.Column<int>(type: "int", nullable: false),
                    UnplacedPackages = table.Column<int>(type: "int", nullable: false),
                    TotalWeightKg = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    UsedWeightKg = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    WeightUtilizationPercent = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    VolumeUtilizationPercent = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    Warnings = table.Column<string>(type: "longtext", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OptimizationRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OptimizationRuns_Shipments_ShipmentId",
                        column: x => x.ShipmentId,
                        principalTable: "Shipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OptimizationRuns_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OptimizationRuns_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ShipmentPackages",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    ShipmentId = table.Column<long>(type: "bigint", nullable: false),
                    Description = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: false),
                    PackageCode = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: true),
                    Category = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    LengthCm = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    WidthCm = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    HeightCm = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    WeightKg = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    IsFragile = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    SpecialHandlingRequirements = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShipmentPackages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShipmentPackages_Shipments_ShipmentId",
                        column: x => x.ShipmentId,
                        principalTable: "Shipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "LoadingPlanApprovals",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    OptimizationRunId = table.Column<long>(type: "bigint", nullable: false),
                    ReviewedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    Decision = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    Comments = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true),
                    ReviewedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoadingPlanApprovals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoadingPlanApprovals_OptimizationRuns_OptimizationRunId",
                        column: x => x.OptimizationRunId,
                        principalTable: "OptimizationRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LoadingPlanApprovals_Users_ReviewedByUserId",
                        column: x => x.ReviewedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PackagePlacements",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    OptimizationRunId = table.Column<long>(type: "bigint", nullable: false),
                    ShipmentPackageId = table.Column<long>(type: "bigint", nullable: false),
                    UnitNumber = table.Column<int>(type: "int", nullable: false),
                    PackageDescription = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: false),
                    LengthCm = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    WidthCm = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    HeightCm = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    WeightKg = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    IsFragile = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    PlacementStatus = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    X = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    Y = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    Z = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    Orientation = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: true),
                    UnplacedReason = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackagePlacements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PackagePlacements_OptimizationRuns_OptimizationRunId",
                        column: x => x.OptimizationRunId,
                        principalTable: "OptimizationRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PackagePlacements_ShipmentPackages_ShipmentPackageId",
                        column: x => x.ShipmentPackageId,
                        principalTable: "ShipmentPackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_LoadingPlanApprovals_OptimizationRunId",
                table: "LoadingPlanApprovals",
                column: "OptimizationRunId");

            migrationBuilder.CreateIndex(
                name: "IX_LoadingPlanApprovals_ReviewedAtUtc",
                table: "LoadingPlanApprovals",
                column: "ReviewedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_LoadingPlanApprovals_ReviewedByUserId",
                table: "LoadingPlanApprovals",
                column: "ReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OptimizationRuns_CreatedAtUtc",
                table: "OptimizationRuns",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_OptimizationRuns_CreatedByUserId",
                table: "OptimizationRuns",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OptimizationRuns_ShipmentId",
                table: "OptimizationRuns",
                column: "ShipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_OptimizationRuns_VehicleId",
                table: "OptimizationRuns",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_PackagePlacements_OptimizationRunId",
                table: "PackagePlacements",
                column: "OptimizationRunId");

            migrationBuilder.CreateIndex(
                name: "IX_PackagePlacements_OptimizationRunId_ShipmentPackageId_UnitNu~",
                table: "PackagePlacements",
                columns: new[] { "OptimizationRunId", "ShipmentPackageId", "UnitNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PackagePlacements_ShipmentPackageId",
                table: "PackagePlacements",
                column: "ShipmentPackageId");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentPackages_ShipmentId",
                table: "ShipmentPackages",
                column: "ShipmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LoadingPlanApprovals");

            migrationBuilder.DropTable(
                name: "PackagePlacements");

            migrationBuilder.DropTable(
                name: "OptimizationRuns");

            migrationBuilder.DropTable(
                name: "ShipmentPackages");

            migrationBuilder.DropColumn(
                name: "CargoHeightCm",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "CargoLengthCm",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "CargoWidthCm",
                table: "Vehicles");
        }
    }
}
