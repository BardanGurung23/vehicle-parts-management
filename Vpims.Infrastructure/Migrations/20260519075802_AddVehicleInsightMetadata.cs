using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vpims.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleInsightMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_service_date",
                table: "vehicles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "manufacture_year",
                table: "vehicles",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "mileage",
                table: "vehicles",
                type: "integer",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_vehicles_manufacture_year_range",
                table: "vehicles",
                sql: "manufacture_year IS NULL OR (manufacture_year >= 1950 AND manufacture_year <= 2100)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_vehicles_mileage_non_negative",
                table: "vehicles",
                sql: "mileage IS NULL OR mileage >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_vehicles_manufacture_year_range",
                table: "vehicles");

            migrationBuilder.DropCheckConstraint(
                name: "ck_vehicles_mileage_non_negative",
                table: "vehicles");

            migrationBuilder.DropColumn(
                name: "last_service_date",
                table: "vehicles");

            migrationBuilder.DropColumn(
                name: "manufacture_year",
                table: "vehicles");

            migrationBuilder.DropColumn(
                name: "mileage",
                table: "vehicles");
        }
    }
}
