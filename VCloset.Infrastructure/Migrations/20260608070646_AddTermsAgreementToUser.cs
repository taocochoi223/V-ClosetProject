using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VCloset.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTermsAgreementToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "agreed_to_terms_at",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "agreed_to_terms_ip",
                table: "users",
                type: "character varying(45)",
                maxLength: 45,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "terms_version",
                table: "users",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "subscription_plans",
                keyColumn: "internal_id",
                keyValue: 1,
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTime(2026, 6, 8, 7, 6, 44, 561, DateTimeKind.Utc).AddTicks(358), new DateTime(2026, 6, 8, 7, 6, 44, 561, DateTimeKind.Utc).AddTicks(361) });

            migrationBuilder.UpdateData(
                table: "subscription_plans",
                keyColumn: "internal_id",
                keyValue: 2,
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTime(2026, 6, 8, 7, 6, 44, 561, DateTimeKind.Utc).AddTicks(365), new DateTime(2026, 6, 8, 7, 6, 44, 561, DateTimeKind.Utc).AddTicks(365) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "agreed_to_terms_at",
                table: "users");

            migrationBuilder.DropColumn(
                name: "agreed_to_terms_ip",
                table: "users");

            migrationBuilder.DropColumn(
                name: "terms_version",
                table: "users");

            migrationBuilder.UpdateData(
                table: "subscription_plans",
                keyColumn: "internal_id",
                keyValue: 1,
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTime(2026, 6, 4, 6, 42, 52, 695, DateTimeKind.Utc).AddTicks(33), new DateTime(2026, 6, 4, 6, 42, 52, 695, DateTimeKind.Utc).AddTicks(52) });

            migrationBuilder.UpdateData(
                table: "subscription_plans",
                keyColumn: "internal_id",
                keyValue: 2,
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTime(2026, 6, 4, 6, 42, 52, 695, DateTimeKind.Utc).AddTicks(67), new DateTime(2026, 6, 4, 6, 42, 52, 695, DateTimeKind.Utc).AddTicks(67) });
        }
    }
}
