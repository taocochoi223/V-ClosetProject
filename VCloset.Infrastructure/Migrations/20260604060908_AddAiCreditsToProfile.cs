using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VCloset.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAiCreditsToProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "wardrobe_item_count",
                table: "customer_profiles",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                comment: "Cache d? check gi?i h?n freemium 50 items m khng COUNT(*).",
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0,
                oldComment: "Cache d? check gi?i h?n freemium 50 items m� kh�ng COUNT(*).");

            migrationBuilder.AddColumn<int>(
                name: "bg_removal_credits",
                table: "customer_profiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "try_on_credits",
                table: "customer_profiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "subscription_plans",
                keyColumn: "internal_id",
                keyValue: 1,
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTime(2026, 6, 4, 6, 9, 6, 172, DateTimeKind.Utc).AddTicks(4282), new DateTime(2026, 6, 4, 6, 9, 6, 172, DateTimeKind.Utc).AddTicks(4286) });

            migrationBuilder.UpdateData(
                table: "subscription_plans",
                keyColumn: "internal_id",
                keyValue: 2,
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTime(2026, 6, 4, 6, 9, 6, 172, DateTimeKind.Utc).AddTicks(4291), new DateTime(2026, 6, 4, 6, 9, 6, 172, DateTimeKind.Utc).AddTicks(4292) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "bg_removal_credits",
                table: "customer_profiles");

            migrationBuilder.DropColumn(
                name: "try_on_credits",
                table: "customer_profiles");

            migrationBuilder.AlterColumn<int>(
                name: "wardrobe_item_count",
                table: "customer_profiles",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                comment: "Cache d? check gi?i h?n freemium 50 items m� kh�ng COUNT(*).",
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0,
                oldComment: "Cache d? check gi?i h?n freemium 50 items m khng COUNT(*).");

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
    }
}
