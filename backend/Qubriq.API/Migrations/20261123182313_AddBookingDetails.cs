using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qubriq.API.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdditionalInstructions",
                table: "Shipments",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CargoCategory",
                table: "Shipments",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CargoName",
                table: "Shipments",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryTimeWindow",
                table: "Shipments",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "HeightCm",
                table: "Shipments",
                type: "decimal(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFragile",
                table: "Shipments",
                type: "tinyint(1)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LengthCm",
                table: "Shipments",
                type: "decimal(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PackageCount",
                table: "Shipments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PickupDateUtc",
                table: "Shipments",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Priority",
                table: "Shipments",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SpecialHandlingRequirements",
                table: "Shipments",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalWeightKg",
                table: "Shipments",
                type: "decimal(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WidthCm",
                table: "Shipments",
                type: "decimal(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdditionalInstructions",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "CargoCategory",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "CargoName",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "DeliveryTimeWindow",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "HeightCm",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "IsFragile",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "LengthCm",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "PackageCount",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "PickupDateUtc",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "SpecialHandlingRequirements",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "TotalWeightKg",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "WidthCm",
                table: "Shipments");
        }
    }
}
