using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Vpims.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AppointmentsPartRequestsReviews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_customers_users_user_id",
                table: "customers");

            migrationBuilder.AlterColumn<int>(
                name: "user_id",
                table: "customers",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "email",
                table: "customers",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "full_name",
                table: "customers",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "phone_number",
                table: "customers",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "vehicles",
                columns: table => new
                {
                    vehicle_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    customer_id = table.Column<int>(type: "integer", nullable: false),
                    vehicle_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    model = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vehicles", x => x.vehicle_id);
                    table.ForeignKey(
                        name: "FK_vehicles_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "customer_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "appointments",
                columns: table => new
                {
                    appointment_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    customer_id = table.Column<int>(type: "integer", nullable: false),
                    vehicle_id = table.Column<int>(type: "integer", nullable: false),
                    appointment_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    service_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "Pending"),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_appointments", x => x.appointment_id);
                    table.ForeignKey(
                        name: "FK_appointments_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "customer_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_appointments_vehicles_vehicle_id",
                        column: x => x.vehicle_id,
                        principalTable: "vehicles",
                        principalColumn: "vehicle_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "part_requests",
                columns: table => new
                {
                    request_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    customer_id = table.Column<int>(type: "integer", nullable: false),
                    vehicle_id = table.Column<int>(type: "integer", nullable: true),
                    requested_part_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    request_details = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "Pending"),
                    requested_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    resolved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_part_requests", x => x.request_id);
                    table.ForeignKey(
                        name: "FK_part_requests_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "customer_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_part_requests_vehicles_vehicle_id",
                        column: x => x.vehicle_id,
                        principalTable: "vehicles",
                        principalColumn: "vehicle_id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "service_reviews",
                columns: table => new
                {
                    review_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    appointment_id = table.Column<int>(type: "integer", nullable: false),
                    customer_id = table.Column<int>(type: "integer", nullable: false),
                    rating = table.Column<int>(type: "integer", nullable: false),
                    comment = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_reviews", x => x.review_id);
                    table.ForeignKey(
                        name: "FK_service_reviews_appointments_appointment_id",
                        column: x => x.appointment_id,
                        principalTable: "appointments",
                        principalColumn: "appointment_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_service_reviews_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "customer_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_customers_email",
                table: "customers",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_customers_phone_number",
                table: "customers",
                column: "phone_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_appointments_customer_id",
                table: "appointments",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_appointments_vehicle_id",
                table: "appointments",
                column: "vehicle_id");

            migrationBuilder.CreateIndex(
                name: "ix_part_requests_customer_id",
                table: "part_requests",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_part_requests_vehicle_id",
                table: "part_requests",
                column: "vehicle_id");

            migrationBuilder.CreateIndex(
                name: "ix_service_reviews_appointment_id",
                table: "service_reviews",
                column: "appointment_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_service_reviews_customer_id",
                table: "service_reviews",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_vehicles_customer_id",
                table: "vehicles",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_vehicles_vehicle_number",
                table: "vehicles",
                column: "vehicle_number",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_customers_users_user_id",
                table: "customers",
                column: "user_id",
                principalTable: "users",
                principalColumn: "user_id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_customers_users_user_id",
                table: "customers");

            migrationBuilder.DropTable(
                name: "part_requests");

            migrationBuilder.DropTable(
                name: "service_reviews");

            migrationBuilder.DropTable(
                name: "appointments");

            migrationBuilder.DropTable(
                name: "vehicles");

            migrationBuilder.DropIndex(
                name: "IX_customers_email",
                table: "customers");

            migrationBuilder.DropIndex(
                name: "IX_customers_phone_number",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "email",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "full_name",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "phone_number",
                table: "customers");

            migrationBuilder.AlterColumn<int>(
                name: "user_id",
                table: "customers",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_customers_users_user_id",
                table: "customers",
                column: "user_id",
                principalTable: "users",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
