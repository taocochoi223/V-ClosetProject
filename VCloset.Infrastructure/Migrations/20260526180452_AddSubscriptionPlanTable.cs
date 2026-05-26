using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace VCloset.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionPlanTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "subscription_plan_internal_id",
                table: "premium_subscriptions",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "subscription_plans",
                columns: table => new
                {
                    internal_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    price = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "VND"),
                    duration_days = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("subscription_plans_pkey", x => x.internal_id);
                },
                comment: "Bảng cấu hình gói Premium phục vụ thanh toán.");

            migrationBuilder.InsertData(
                table: "subscription_plans",
                columns: new[] { "internal_id", "created_at", "currency", "description", "duration_days", "id", "is_active", "name", "price", "updated_at" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 5, 26, 18, 4, 51, 591, DateTimeKind.Utc).AddTicks(4828), "VND", "Mở khóa toàn bộ tính năng và giới hạn tủ đồ trong 30 ngày.", 30, new Guid("3f5f3e9c-502a-43c2-bf72-351faab24c8b"), true, "Gói Tháng Premium", 49000m, new DateTime(2026, 5, 26, 18, 4, 51, 591, DateTimeKind.Utc).AddTicks(4830) },
                    { 2, new DateTime(2026, 5, 26, 18, 4, 51, 591, DateTimeKind.Utc).AddTicks(4842), "VND", "Mở khóa toàn bộ tính năng và giới hạn tủ đồ trong 365 ngày (Tiết kiệm hơn).", 365, new Guid("b0d61ca5-408a-4084-9dbb-8cd9c13b19ff"), true, "Gói Năm Premium", 399000m, new DateTime(2026, 5, 26, 18, 4, 51, 591, DateTimeKind.Utc).AddTicks(4843) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_premium_subscriptions_subscription_plan_internal_id",
                table: "premium_subscriptions",
                column: "subscription_plan_internal_id");

            migrationBuilder.CreateIndex(
                name: "subscription_plans_id_key",
                table: "subscription_plans",
                column: "id",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "premium_subscriptions_subscription_plan_internal_id_fkey",
                table: "premium_subscriptions",
                column: "subscription_plan_internal_id",
                principalTable: "subscription_plans",
                principalColumn: "internal_id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "premium_subscriptions_subscription_plan_internal_id_fkey",
                table: "premium_subscriptions");

            migrationBuilder.DropTable(
                name: "subscription_plans");

            migrationBuilder.DropIndex(
                name: "IX_premium_subscriptions_subscription_plan_internal_id",
                table: "premium_subscriptions");

            migrationBuilder.DropColumn(
                name: "subscription_plan_internal_id",
                table: "premium_subscriptions");
        }
    }
}
