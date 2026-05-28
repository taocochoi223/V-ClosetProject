using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using VCloset.Domain.Enums;

#nullable disable

namespace VCloset.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentTransactionTable : Migration
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
                .OldAnnotation("Npgsql:Enum:premium_plan", "monthly,yearly")
                .OldAnnotation("Npgsql:Enum:user_role", "customer,admin,moderator,brand_partner")
                .OldAnnotation("Npgsql:PostgresExtension:pgcrypto", ",,");

            migrationBuilder.CreateTable(
                name: "payment_transactions",
                columns: table => new
                {
                    internal_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_internal_id = table.Column<int>(type: "integer", nullable: false),
                    subscription_plan_internal_id = table.Column<int>(type: "integer", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, defaultValueSql: "'VND'::character varying"),
                    payment_gateway = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<PaymentStatus>(type: "payment_status", nullable: false),
                    gateway_transaction_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    raw_callback_data = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("payment_transactions_pkey", x => x.internal_id);
                    table.ForeignKey(
                        name: "payment_transactions_subscription_plan_internal_id_fkey",
                        column: x => x.subscription_plan_internal_id,
                        principalTable: "subscription_plans",
                        principalColumn: "internal_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "payment_transactions_user_internal_id_fkey",
                        column: x => x.user_internal_id,
                        principalTable: "users",
                        principalColumn: "internal_id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Bảng ghi nhận giao dịch thanh toán qua ví điện tử");

            migrationBuilder.UpdateData(
                table: "subscription_plans",
                keyColumn: "internal_id",
                keyValue: 1,
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTime(2026, 5, 28, 8, 44, 12, 425, DateTimeKind.Utc).AddTicks(7336), new DateTime(2026, 5, 28, 8, 44, 12, 425, DateTimeKind.Utc).AddTicks(7338) });

            migrationBuilder.UpdateData(
                table: "subscription_plans",
                keyColumn: "internal_id",
                keyValue: 2,
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTime(2026, 5, 28, 8, 44, 12, 425, DateTimeKind.Utc).AddTicks(7340), new DateTime(2026, 5, 28, 8, 44, 12, 425, DateTimeKind.Utc).AddTicks(7341) });

            migrationBuilder.CreateIndex(
                name: "IX_payment_transactions_subscription_plan_internal_id",
                table: "payment_transactions",
                column: "subscription_plan_internal_id");

            migrationBuilder.CreateIndex(
                name: "IX_payment_transactions_user_internal_id",
                table: "payment_transactions",
                column: "user_internal_id");

            migrationBuilder.CreateIndex(
                name: "payment_transactions_id_key",
                table: "payment_transactions",
                column: "id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "payment_transactions");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:ai_job_status", "pending,processing,completed,failed")
                .Annotation("Npgsql:Enum:auth_provider", "local,google")
                .Annotation("Npgsql:Enum:body_shape_type", "hourglass,pear,apple,rectangle,inverted_triangle")
                .Annotation("Npgsql:Enum:brand_status", "pending,verified,suspended")
                .Annotation("Npgsql:Enum:chat_room_type", "public,topic,direct")
                .Annotation("Npgsql:Enum:clothing_category", "top,bottom,dress,outerwear,shoes,bag,accessory,other")
                .Annotation("Npgsql:Enum:commission_status", "pending,confirmed,paid,rejected")
                .Annotation("Npgsql:Enum:message_type", "text,image,outfit_share,system")
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

            migrationBuilder.UpdateData(
                table: "subscription_plans",
                keyColumn: "internal_id",
                keyValue: 1,
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTime(2026, 5, 26, 18, 4, 51, 591, DateTimeKind.Utc).AddTicks(4828), new DateTime(2026, 5, 26, 18, 4, 51, 591, DateTimeKind.Utc).AddTicks(4830) });

            migrationBuilder.UpdateData(
                table: "subscription_plans",
                keyColumn: "internal_id",
                keyValue: 2,
                columns: new[] { "created_at", "updated_at" },
                values: new object[] { new DateTime(2026, 5, 26, 18, 4, 51, 591, DateTimeKind.Utc).AddTicks(4842), new DateTime(2026, 5, 26, 18, 4, 51, 591, DateTimeKind.Utc).AddTicks(4843) });
        }
    }
}
