using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using VCloset.Domain.Enums;

#nullable disable

namespace VCloset.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClosetFolders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "closet_internal_id",
                table: "wardrobe_items",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql(@"
                ALTER TABLE coupons 
                ALTER COLUMN discount_type TYPE discount_type 
                USING (
                    CASE discount_type
                        WHEN 1 THEN 'percentage'::discount_type
                        WHEN 2 THEN 'fixed_amount'::discount_type
                        ELSE 'percentage'::discount_type
                    END
                );
            ");

            migrationBuilder.CreateTable(
                name: "closets",
                columns: table => new
                {
                    internal_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_internal_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("closets_pkey", x => x.internal_id);
                    table.ForeignKey(
                        name: "closets_user_internal_id_fkey",
                        column: x => x.user_internal_id,
                        principalTable: "users",
                        principalColumn: "internal_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "subscription_plans",
                keyColumn: "internal_id",
                keyValue: 1,
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTime(2026, 6, 26, 6, 8, 57, 436, DateTimeKind.Utc).AddTicks(1483), new DateTime(2026, 6, 26, 6, 8, 57, 436, DateTimeKind.Utc).AddTicks(1487) });

            migrationBuilder.UpdateData(
                table: "subscription_plans",
                keyColumn: "internal_id",
                keyValue: 2,
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTime(2026, 6, 26, 6, 8, 57, 436, DateTimeKind.Utc).AddTicks(1493), new DateTime(2026, 6, 26, 6, 8, 57, 436, DateTimeKind.Utc).AddTicks(1494) });

            migrationBuilder.CreateIndex(
                name: "IX_wardrobe_items_closet_internal_id",
                table: "wardrobe_items",
                column: "closet_internal_id");

            migrationBuilder.CreateIndex(
                name: "closets_id_key",
                table: "closets",
                column: "id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_closets_user",
                table: "closets",
                column: "user_internal_id");

            migrationBuilder.AddForeignKey(
                name: "wardrobe_items_closet_internal_id_fkey",
                table: "wardrobe_items",
                column: "closet_internal_id",
                principalTable: "closets",
                principalColumn: "internal_id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "wardrobe_items_closet_internal_id_fkey",
                table: "wardrobe_items");

            migrationBuilder.DropTable(
                name: "closets");

            migrationBuilder.DropIndex(
                name: "IX_wardrobe_items_closet_internal_id",
                table: "wardrobe_items");

            migrationBuilder.DropColumn(
                name: "closet_internal_id",
                table: "wardrobe_items");

            migrationBuilder.Sql(@"
                ALTER TABLE coupons 
                ALTER COLUMN discount_type TYPE integer 
                USING (
                    CASE discount_type
                        WHEN 'percentage'::discount_type THEN 1
                        WHEN 'fixed_amount'::discount_type THEN 2
                        ELSE 1
                    END
                );
            ");

            migrationBuilder.UpdateData(
                table: "subscription_plans",
                keyColumn: "internal_id",
                keyValue: 1,
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTime(2026, 6, 25, 6, 26, 22, 437, DateTimeKind.Utc).AddTicks(7682), new DateTime(2026, 6, 25, 6, 26, 22, 437, DateTimeKind.Utc).AddTicks(7685) });

            migrationBuilder.UpdateData(
                table: "subscription_plans",
                keyColumn: "internal_id",
                keyValue: 2,
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTime(2026, 6, 25, 6, 26, 22, 437, DateTimeKind.Utc).AddTicks(7689), new DateTime(2026, 6, 25, 6, 26, 22, 437, DateTimeKind.Utc).AddTicks(7690) });
        }
    }
}
