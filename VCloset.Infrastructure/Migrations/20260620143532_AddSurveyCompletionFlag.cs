using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VCloset.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSurveyCompletionFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "has_completed_survey",
                table: "customer_profiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "subscription_plans",
                keyColumn: "internal_id",
                keyValue: 1,
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTime(2026, 6, 20, 14, 35, 30, 702, DateTimeKind.Utc).AddTicks(3637), new DateTime(2026, 6, 20, 14, 35, 30, 702, DateTimeKind.Utc).AddTicks(3640) });

            migrationBuilder.UpdateData(
                table: "subscription_plans",
                keyColumn: "internal_id",
                keyValue: 2,
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTime(2026, 6, 20, 14, 35, 30, 702, DateTimeKind.Utc).AddTicks(3645), new DateTime(2026, 6, 20, 14, 35, 30, 702, DateTimeKind.Utc).AddTicks(3645) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "has_completed_survey",
                table: "customer_profiles");

            migrationBuilder.UpdateData(
                table: "subscription_plans",
                keyColumn: "internal_id",
                keyValue: 1,
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTime(2026, 6, 13, 5, 26, 50, 607, DateTimeKind.Utc).AddTicks(9181), new DateTime(2026, 6, 13, 5, 26, 50, 607, DateTimeKind.Utc).AddTicks(9184) });

            migrationBuilder.UpdateData(
                table: "subscription_plans",
                keyColumn: "internal_id",
                keyValue: 2,
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTime(2026, 6, 13, 5, 26, 50, 607, DateTimeKind.Utc).AddTicks(9189), new DateTime(2026, 6, 13, 5, 26, 50, 607, DateTimeKind.Utc).AddTicks(9189) });
        }
    }
}
