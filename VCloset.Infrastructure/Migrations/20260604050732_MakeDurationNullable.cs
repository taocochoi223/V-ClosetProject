using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VCloset.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MakeDurationNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "duration_days",
                table: "subscription_plans",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<DateTime>(
                name: "expires_at",
                table: "premium_subscriptions",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");



            migrationBuilder.UpdateData(
                table: "subscription_plans",
                keyColumn: "internal_id",
                keyValue: 1,
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTime(2026, 6, 4, 5, 7, 30, 98, DateTimeKind.Utc).AddTicks(909), new DateTime(2026, 6, 4, 5, 7, 30, 98, DateTimeKind.Utc).AddTicks(911) });

            migrationBuilder.UpdateData(
                table: "subscription_plans",
                keyColumn: "internal_id",
                keyValue: 2,
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTime(2026, 6, 4, 5, 7, 30, 98, DateTimeKind.Utc).AddTicks(914), new DateTime(2026, 6, 4, 5, 7, 30, 98, DateTimeKind.Utc).AddTicks(915) });


        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {


            migrationBuilder.AlterColumn<int>(
                name: "duration_days",
                table: "subscription_plans",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "expires_at",
                table: "premium_subscriptions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "subscription_plans",
                keyColumn: "internal_id",
                keyValue: 1,
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTime(2026, 5, 28, 8, 44, 12, 425, DateTimeKind.Utc).AddTicks(7336), new DateTime(2026, 5, 28, 8, 44, 12, 425, DateTimeKind.Utc).AddTicks(7338) });

            migrationBuilder.UpdateData(
                table: "subscription_plans",
                keyColumn: "internal_id",
                keyValue: 2,
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTime(2026, 5, 28, 8, 44, 12, 425, DateTimeKind.Utc).AddTicks(7340), new DateTime(2026, 5, 28, 8, 44, 12, 425, DateTimeKind.Utc).AddTicks(7341) });
        }
    }
}
