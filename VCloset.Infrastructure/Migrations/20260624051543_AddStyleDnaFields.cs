using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VCloset.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStyleDnaFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "body_type",
                table: "customer_profiles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "color_pref",
                table: "customer_profiles",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "skin_tone",
                table: "customer_profiles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "style_pref",
                table: "customer_profiles",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "subscription_plans",
                keyColumn: "internal_id",
                keyValue: 1,
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTime(2026, 6, 24, 5, 15, 41, 726, DateTimeKind.Utc).AddTicks(3130), new DateTime(2026, 6, 24, 5, 15, 41, 726, DateTimeKind.Utc).AddTicks(3134) });

            migrationBuilder.UpdateData(
                table: "subscription_plans",
                keyColumn: "internal_id",
                keyValue: 2,
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTime(2026, 6, 24, 5, 15, 41, 726, DateTimeKind.Utc).AddTicks(3142), new DateTime(2026, 6, 24, 5, 15, 41, 726, DateTimeKind.Utc).AddTicks(3142) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "body_type",
                table: "customer_profiles");

            migrationBuilder.DropColumn(
                name: "color_pref",
                table: "customer_profiles");

            migrationBuilder.DropColumn(
                name: "skin_tone",
                table: "customer_profiles");

            migrationBuilder.DropColumn(
                name: "style_pref",
                table: "customer_profiles");

            migrationBuilder.UpdateData(
                table: "subscription_plans",
                keyColumn: "internal_id",
                keyValue: 1,
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTime(2026, 6, 22, 8, 36, 14, 543, DateTimeKind.Utc).AddTicks(7494), new DateTime(2026, 6, 22, 8, 36, 14, 543, DateTimeKind.Utc).AddTicks(7497) });

            migrationBuilder.UpdateData(
                table: "subscription_plans",
                keyColumn: "internal_id",
                keyValue: 2,
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTime(2026, 6, 22, 8, 36, 14, 543, DateTimeKind.Utc).AddTicks(7504), new DateTime(2026, 6, 22, 8, 36, 14, 543, DateTimeKind.Utc).AddTicks(7504) });
        }
    }
}
