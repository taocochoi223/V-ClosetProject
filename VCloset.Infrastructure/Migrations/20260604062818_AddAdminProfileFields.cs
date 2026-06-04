using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VCloset.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminProfileFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EmployeeCode",
                table: "admin_profiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JobTitle",
                table: "admin_profiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "admin_profiles",
                type: "text",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "subscription_plans",
                keyColumn: "internal_id",
                keyValue: 1,
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTime(2026, 6, 4, 6, 28, 17, 968, DateTimeKind.Utc).AddTicks(7104), new DateTime(2026, 6, 4, 6, 28, 17, 968, DateTimeKind.Utc).AddTicks(7106) });

            migrationBuilder.UpdateData(
                table: "subscription_plans",
                keyColumn: "internal_id",
                keyValue: 2,
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTime(2026, 6, 4, 6, 28, 17, 968, DateTimeKind.Utc).AddTicks(7109), new DateTime(2026, 6, 4, 6, 28, 17, 968, DateTimeKind.Utc).AddTicks(7110) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmployeeCode",
                table: "admin_profiles");

            migrationBuilder.DropColumn(
                name: "JobTitle",
                table: "admin_profiles");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "admin_profiles");

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
