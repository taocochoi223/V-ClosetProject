using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace VCloset.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCouponsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:ai_job_status", "pending,processing,completed,failed")
                .Annotation("Npgsql:Enum:auth_provider", "local,google")
                .Annotation("Npgsql:Enum:body_shape_type", "hourglass,pear,apple,rectangle,inverted_triangle")
                .Annotation("Npgsql:Enum:brand_status", "pending,verified,suspended")
                .Annotation("Npgsql:Enum:chat_room_type", "public,topic,direct")
                .Annotation("Npgsql:Enum:clothing_category", "top,bottom,dress,outerwear,shoes,bag,accessory,other")
                .Annotation("Npgsql:Enum:commission_status", "pending,confirmed,paid,rejected")
                .Annotation("Npgsql:Enum:discount_type", "percentage,fixed_amount")
                .Annotation("Npgsql:Enum:message_type", "text,image,outfit_share,system")
                .Annotation("Npgsql:Enum:payment_status", "pending,success,failed,cancelled,expired")
                .Annotation("Npgsql:Enum:premium_plan", "monthly,yearly")
                .Annotation("Npgsql:Enum:user_role", "customer,admin,moderator,brand_partner")
                .Annotation("Npgsql:PostgresExtension:pgcrypto", ",,")
                .OldAnnotation("Npgsql:Enum:ai_job_status", "pending,processing,completed,failed")
                .OldAnnotation("Npgsql:Enum:auth_provider", "local,google")
                .OldAnnotation("Npgsql:Enum:body_shape_type", "hourglass,pear,apple,rectangle,inverted_triangle")
                .OldAnnotation("Npgsql:Enum:brand_status", "pending,verified,suspended")
                .OldAnnotation("Npgsql:Enum:chat_room_type", "public,topic,direct")
                .OldAnnotation("Npgsql:Enum:clothing_category", "top,bottom,dress,outerwear,shoes,bag,accessory,other")
                .OldAnnotation("Npgsql:Enum:commission_status", "pending,confirmed,paid,rejected")
                .OldAnnotation("Npgsql:Enum:message_type", "text,image,outfit_share,system")
                .OldAnnotation("Npgsql:Enum:payment_status", "pending,success,failed,cancelled,expired")
                .OldAnnotation("Npgsql:Enum:premium_plan", "monthly,yearly")
                .OldAnnotation("Npgsql:Enum:user_role", "customer,admin,moderator,brand_partner")
                .OldAnnotation("Npgsql:PostgresExtension:pgcrypto", ",,");

            migrationBuilder.AddColumn<string>(
                name: "AppliedCouponCode",
                table: "payment_transactions",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "coupons",
                columns: table => new
                {
                    internal_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    discount_type = table.Column<int>(type: "integer", nullable: false),
                    discount_value = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    current_uses = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    max_uses = table.Column<int>(type: "integer", nullable: true),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("coupons_pkey", x => x.internal_id);
                },
                comment: "Bảng ghi nhận Mã giảm giá");

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

            migrationBuilder.CreateIndex(
                name: "coupons_code_key",
                table: "coupons",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "coupons_id_key",
                table: "coupons",
                column: "id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "coupons");

            migrationBuilder.DropColumn(
                name: "AppliedCouponCode",
                table: "payment_transactions");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:ai_job_status", "pending,processing,completed,failed")
                .Annotation("Npgsql:Enum:auth_provider", "local,google")
                .Annotation("Npgsql:Enum:body_shape_type", "hourglass,pear,apple,rectangle,inverted_triangle")
                .Annotation("Npgsql:Enum:brand_status", "pending,verified,suspended")
                .Annotation("Npgsql:Enum:chat_room_type", "public,topic,direct")
                .Annotation("Npgsql:Enum:clothing_category", "top,bottom,dress,outerwear,shoes,bag,accessory,other")
                .Annotation("Npgsql:Enum:commission_status", "pending,confirmed,paid,rejected")
                .Annotation("Npgsql:Enum:message_type", "text,image,outfit_share,system")
                .Annotation("Npgsql:Enum:payment_status", "pending,success,failed,cancelled,expired")
                .Annotation("Npgsql:Enum:premium_plan", "monthly,yearly")
                .Annotation("Npgsql:Enum:user_role", "customer,admin,moderator,brand_partner")
                .Annotation("Npgsql:PostgresExtension:pgcrypto", ",,")
                .OldAnnotation("Npgsql:Enum:ai_job_status", "pending,processing,completed,failed")
                .OldAnnotation("Npgsql:Enum:auth_provider", "local,google")
                .OldAnnotation("Npgsql:Enum:body_shape_type", "hourglass,pear,apple,rectangle,inverted_triangle")
                .OldAnnotation("Npgsql:Enum:brand_status", "pending,verified,suspended")
                .OldAnnotation("Npgsql:Enum:chat_room_type", "public,topic,direct")
                .OldAnnotation("Npgsql:Enum:clothing_category", "top,bottom,dress,outerwear,shoes,bag,accessory,other")
                .OldAnnotation("Npgsql:Enum:commission_status", "pending,confirmed,paid,rejected")
                .OldAnnotation("Npgsql:Enum:discount_type", "percentage,fixed_amount")
                .OldAnnotation("Npgsql:Enum:message_type", "text,image,outfit_share,system")
                .OldAnnotation("Npgsql:Enum:payment_status", "pending,success,failed,cancelled,expired")
                .OldAnnotation("Npgsql:Enum:premium_plan", "monthly,yearly")
                .OldAnnotation("Npgsql:Enum:user_role", "customer,admin,moderator,brand_partner")
                .OldAnnotation("Npgsql:PostgresExtension:pgcrypto", ",,");

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
