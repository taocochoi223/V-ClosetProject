using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace VCloset.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionTierConfigTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "subscription_tier_configs",
                columns: table => new
                {
                    internal_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tier_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    bg_removal_credits = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    try_on_credits = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    wardrobe_item_limit = table.Column<int>(type: "integer", nullable: true),
                    outfit_limit = table.Column<int>(type: "integer", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subscription_tier_configs", x => x.internal_id);
                });

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

            migrationBuilder.InsertData(
                table: "subscription_tier_configs",
                columns: new[] { "internal_id", "bg_removal_credits", "outfit_limit", "tier_name", "try_on_credits", "updated_at", "updated_by", "wardrobe_item_limit" },
                values: new object[,]
                {
                    { 1, 1, 2, "free", 1, new DateTime(2026, 6, 4, 0, 0, 0, 0, DateTimeKind.Utc), "system", 2 },
                    { 2, 2, null, "premium", 2, new DateTime(2026, 6, 4, 0, 0, 0, 0, DateTimeKind.Utc), "system", null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_subscription_tier_configs_tier_name",
                table: "subscription_tier_configs",
                column: "tier_name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "subscription_tier_configs");

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
    }
}
