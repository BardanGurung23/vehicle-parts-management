using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Vpims.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVendorTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "VendorId",
                table: "parts",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "vendors",
                columns: table => new
                {
                    vendor_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    vendor_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    contact_person = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    phone_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    address = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vendors", x => x.vendor_id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_parts_VendorId",
                table: "parts",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "ix_vendors_vendor_name",
                table: "vendors",
                column: "vendor_name");

            migrationBuilder.AddForeignKey(
                name: "FK_parts_vendors_VendorId",
                table: "parts",
                column: "VendorId",
                principalTable: "vendors",
                principalColumn: "vendor_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_parts_vendors_VendorId",
                table: "parts");

            migrationBuilder.DropTable(
                name: "vendors");

            migrationBuilder.DropIndex(
                name: "IX_parts_VendorId",
                table: "parts");

            migrationBuilder.DropColumn(
                name: "VendorId",
                table: "parts");
        }
    }
}
