using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VCloset.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSystemSettingsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "system_settings",
                columns: table => new
                {
                    setting_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    setting_value = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("system_settings_pkey", x => x.setting_key);
                });

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

            migrationBuilder.InsertData(
                table: "system_settings",
                columns: new[] { "setting_key", "setting_value" },
                values: new object[] { "survey_url", "https://forms.gle/YOUR_GOOGLE_FORM_LINK" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "system_settings");

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
    }
}
