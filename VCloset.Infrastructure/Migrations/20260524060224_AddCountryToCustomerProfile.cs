using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VCloset.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCountryToCustomerProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "country",
                table: "customer_profiles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "country",
                table: "customer_profiles");
        }
    }
}
