using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VCloset.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonalProfileFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "address",
                table: "customer_profiles",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "date_of_birth",
                table: "customer_profiles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "gender",
                table: "customer_profiles",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "phone_number",
                table: "customer_profiles",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "address",
                table: "customer_profiles");

            migrationBuilder.DropColumn(
                name: "date_of_birth",
                table: "customer_profiles");

            migrationBuilder.DropColumn(
                name: "gender",
                table: "customer_profiles");

            migrationBuilder.DropColumn(
                name: "phone_number",
                table: "customer_profiles");
        }
    }
}
