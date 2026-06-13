using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VCloset.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGrantedCreditsToSubscriptionPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "granted_bg_credits",
                table: "subscription_plans",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "granted_try_on_credits",
                table: "subscription_plans",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "subscription_plans",
                keyColumn: "internal_id",
                keyValue: 1,
                columns: new[] { "created_at", "granted_bg_credits", "granted_try_on_credits", "updated_at" },
                values: new object[] { new DateTime(2026, 6, 13, 5, 26, 50, 607, DateTimeKind.Utc).AddTicks(9181), 30, 30, new DateTime(2026, 6, 13, 5, 26, 50, 607, DateTimeKind.Utc).AddTicks(9184) });

            migrationBuilder.UpdateData(
                table: "subscription_plans",
                keyColumn: "internal_id",
                keyValue: 2,
                columns: new[] { "created_at", "granted_bg_credits", "granted_try_on_credits", "updated_at" },
                values: new object[] { new DateTime(2026, 6, 13, 5, 26, 50, 607, DateTimeKind.Utc).AddTicks(9189), 360, 360, new DateTime(2026, 6, 13, 5, 26, 50, 607, DateTimeKind.Utc).AddTicks(9189) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "granted_bg_credits",
                table: "subscription_plans");

            migrationBuilder.DropColumn(
                name: "granted_try_on_credits",
                table: "subscription_plans");

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
    }
}
