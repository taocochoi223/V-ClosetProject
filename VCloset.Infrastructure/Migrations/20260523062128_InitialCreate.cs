using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using VCloset.Domain.Enums;

#nullable disable

namespace VCloset.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
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
                .Annotation("Npgsql:Enum:premium_plan", "monthly,yearly")
                .Annotation("Npgsql:Enum:user_role", "customer,admin,moderator,brand_partner")
                .Annotation("Npgsql:PostgresExtension:pgcrypto", ",,");

            migrationBuilder.CreateTable(
                name: "affiliate_products",
                columns: table => new
                {
                    internal_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    shopee_product_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    shopee_shop_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    image_url = table.Column<string>(type: "text", nullable: false),
                    price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    original_price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    affiliate_link = table.Column<string>(type: "text", nullable: false),
                    tracking_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    click_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    conversion_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_trending = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    category = table.Column<ClothingCategory>(type: "clothing_category", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("affiliate_products_pkey", x => x.internal_id);
                },
                comment: "S?n ph?m trending sync t? Shopee m?i d�m. T?o tru?c canvas_outfit_items v� c� FK ph? thu?c.");

            migrationBuilder.CreateTable(
                name: "permission_levels",
                columns: table => new
                {
                    id = table.Column<short>(type: "smallint", nullable: false),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("permission_levels_pkey", x => x.id);
                },
                comment: "C?p quy?n t?ng th? cho admin/moderator. FK t? admin_profiles.");

            migrationBuilder.CreateTable(
                name: "permissions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    grp = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("permissions_pkey", x => x.id);
                },
                comment: "Danh m?c permission. code d?ng group.action d�ng trong C# RequirePermission attribute.");

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    internal_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    google_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    display_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    avatar_url = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    is_email_verified = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    role = table.Column<UserRole>(type: "user_role", nullable: false),
                    auth_provider = table.Column<AuthProvider>(type: "auth_provider", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("users_pkey", x => x.internal_id);
                },
                comment: "B?ng g?c t�i kho?n. internal_id l� PK th?t d�ng cho FK. id UUID ch? d�ng cho API/URL.");

            migrationBuilder.CreateTable(
                name: "permission_level_defaults",
                columns: table => new
                {
                    permission_level_id = table.Column<short>(type: "smallint", nullable: false),
                    permission_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("permission_level_defaults_pkey", x => new { x.permission_level_id, x.permission_id });
                    table.ForeignKey(
                        name: "permission_level_defaults_permission_id_fkey",
                        column: x => x.permission_id,
                        principalTable: "permissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "permission_level_defaults_permission_level_id_fkey",
                        column: x => x.permission_level_id,
                        principalTable: "permission_levels",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Permission m?c d?nh theo level. Backend seed admin_permissions t? d�y khi t?o admin m?i.");

            migrationBuilder.CreateTable(
                name: "admin_permissions",
                columns: table => new
                {
                    user_internal_id = table.Column<int>(type: "integer", nullable: false),
                    permission_id = table.Column<int>(type: "integer", nullable: false),
                    granted_by_internal = table.Column<int>(type: "integer", nullable: false),
                    granted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("admin_permissions_pkey", x => new { x.user_internal_id, x.permission_id });
                    table.ForeignKey(
                        name: "admin_permissions_granted_by_internal_fkey",
                        column: x => x.granted_by_internal,
                        principalTable: "users",
                        principalColumn: "internal_id");
                    table.ForeignKey(
                        name: "admin_permissions_permission_id_fkey",
                        column: x => x.permission_id,
                        principalTable: "permissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "admin_permissions_user_internal_id_fkey",
                        column: x => x.user_internal_id,
                        principalTable: "users",
                        principalColumn: "internal_id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Permission c? th? t?ng admin. Composite PK INT. granted_by_internal l� audit trail.");

            migrationBuilder.CreateTable(
                name: "admin_profiles",
                columns: table => new
                {
                    internal_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_internal_id = table.Column<int>(type: "integer", nullable: false),
                    permission_level = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)1),
                    department = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("admin_profiles_pkey", x => x.internal_id);
                    table.ForeignKey(
                        name: "admin_profiles_permission_level_fkey",
                        column: x => x.permission_level,
                        principalTable: "permission_levels",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "admin_profiles_user_internal_id_fkey",
                        column: x => x.user_internal_id,
                        principalTable: "users",
                        principalColumn: "internal_id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Profile admin/moderator. permission_level l� vai tr� t?ng th?, chi ti?t ? admin_permissions.");

            migrationBuilder.CreateTable(
                name: "brand_profiles",
                columns: table => new
                {
                    internal_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_internal_id = table.Column<int>(type: "integer", nullable: false),
                    brand_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    logo_url = table.Column<string>(type: "text", nullable: true),
                    website_url = table.Column<string>(type: "text", nullable: true),
                    contact_phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    tax_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    credit_balance = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    status = table.Column<BrandStatus>(type: "brand_status", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("brand_profiles_pkey", x => x.internal_id);
                    table.ForeignKey(
                        name: "brand_profiles_user_internal_id_fkey",
                        column: x => x.user_internal_id,
                        principalTable: "users",
                        principalColumn: "internal_id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Profile brand partner B2B. Admin verify tru?c khi ch?y sponsored campaign.");

            migrationBuilder.CreateTable(
                name: "canvas_outfits",
                columns: table => new
                {
                    internal_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_internal_id = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    canvas_snapshot_url = table.Column<string>(type: "text", nullable: true),
                    is_public = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    like_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("canvas_outfits_pkey", x => x.internal_id);
                    table.ForeignKey(
                        name: "canvas_outfits_user_internal_id_fkey",
                        column: x => x.user_internal_id,
                        principalTable: "users",
                        principalColumn: "internal_id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Outfit t?o t? Canvas 2D. Ch?a d? t? t? nh� v� d? trending affiliate.");

            migrationBuilder.CreateTable(
                name: "chat_rooms",
                columns: table => new
                {
                    internal_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    cover_url = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_by_internal = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    room_type = table.Column<ChatRoomType>(type: "chat_room_type", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("chat_rooms_pkey", x => x.internal_id);
                    table.ForeignKey(
                        name: "chat_rooms_created_by_internal_fkey",
                        column: x => x.created_by_internal,
                        principalTable: "users",
                        principalColumn: "internal_id",
                        onDelete: ReferentialAction.SetNull);
                },
                comment: "Ph�ng chat: public, topic (theo ch? d? th?i trang), direct (2 ngu?i).");

            migrationBuilder.CreateTable(
                name: "customer_profiles",
                columns: table => new
                {
                    internal_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_internal_id = table.Column<int>(type: "integer", nullable: false),
                    height_cm = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    weight_kg = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    mannequin_image_url = table.Column<string>(type: "text", nullable: true),
                    mannequin_generated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    wardrobe_item_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0, comment: "Cache d? check gi?i h?n freemium 50 items m� kh�ng COUNT(*)."),
                    is_chat_banned = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "TRUE = b? kho� chat. K?t h?p chat_banned_until ph�n bi?t t?m th?i/vinh vi?n."),
                    is_post_banned = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "TRUE = b? kho� dang b�i. K?t h?p post_banned_until ph�n bi?t t?m th?i/vinh vi?n."),
                    chat_banned_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    post_banned_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    body_shape = table.Column<BodyShapeType>(type: "body_shape_type", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("customer_profiles_pkey", x => x.internal_id);
                    table.ForeignKey(
                        name: "customer_profiles_user_internal_id_fkey",
                        column: x => x.user_internal_id,
                        principalTable: "users",
                        principalColumn: "internal_id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Profile customer: s? do, mannequin AI, tr?ng th�i ban. FK d�ng INT.");

            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    internal_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_internal_id = table.Column<int>(type: "integer", nullable: false),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    body = table.Column<string>(type: "text", nullable: true),
                    reference_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    reference_id = table.Column<int>(type: "integer", nullable: true),
                    is_read = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("notifications_pkey", x => x.internal_id);
                    table.ForeignKey(
                        name: "notifications_user_internal_id_fkey",
                        column: x => x.user_internal_id,
                        principalTable: "users",
                        principalColumn: "internal_id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Th�ng b�o in-app. reference_id l� internal_id c?a object li�n quan.");

            migrationBuilder.CreateTable(
                name: "premium_subscriptions",
                columns: table => new
                {
                    internal_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_internal_id = table.Column<int>(type: "integer", nullable: false),
                    price_paid = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, defaultValueSql: "'VND'::character varying"),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    payment_method = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    payment_ref = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    cancelled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    plan_type = table.Column<PremiumPlan>(type: "premium_plan", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("premium_subscriptions_pkey", x => x.internal_id);
                    table.ForeignKey(
                        name: "premium_subscriptions_user_internal_id_fkey",
                        column: x => x.user_internal_id,
                        principalTable: "users",
                        principalColumn: "internal_id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "G�i Premium. Check is_active + expires_at d? enforce gi?i h?n freemium.");

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_internal_id = table.Column<int>(type: "integer", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    device_info = table.Column<string>(type: "text", nullable: true),
                    ip_address = table.Column<IPAddress>(type: "inet", nullable: true),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("refresh_tokens_pkey", x => x.id);
                    table.ForeignKey(
                        name: "refresh_tokens_user_internal_id_fkey",
                        column: x => x.user_internal_id,
                        principalTable: "users",
                        principalColumn: "internal_id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "JWT refresh token theo thi?t b?. Logout t? xa, revoke token b?t thu?ng.");

            migrationBuilder.CreateTable(
                name: "user_ban_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_internal_id = table.Column<int>(type: "integer", nullable: false),
                    banned_by_internal = table.Column<int>(type: "integer", nullable: false),
                    ban_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    reason = table.Column<string>(type: "text", nullable: false),
                    banned_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_lifted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    lifted_by_internal = table.Column<int>(type: "integer", nullable: true),
                    lifted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    lift_reason = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("user_ban_logs_pkey", x => x.id);
                    table.ForeignKey(
                        name: "user_ban_logs_banned_by_internal_fkey",
                        column: x => x.banned_by_internal,
                        principalTable: "users",
                        principalColumn: "internal_id");
                    table.ForeignKey(
                        name: "user_ban_logs_lifted_by_internal_fkey",
                        column: x => x.lifted_by_internal,
                        principalTable: "users",
                        principalColumn: "internal_id");
                    table.ForeignKey(
                        name: "user_ban_logs_user_internal_id_fkey",
                        column: x => x.user_internal_id,
                        principalTable: "users",
                        principalColumn: "internal_id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "L?ch s? kho�/m? kho�. Audit log d? moderator gi?i tr�nh v� xem pattern vi ph?m.");

            migrationBuilder.CreateTable(
                name: "wardrobe_items",
                columns: table => new
                {
                    internal_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_internal_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    original_image_url = table.Column<string>(type: "text", nullable: false),
                    removed_bg_url = table.Column<string>(type: "text", nullable: true),
                    color_tags = table.Column<List<string>>(type: "text[]", nullable: true),
                    brand = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    bg_removal_status = table.Column<AiJobStatus>(type: "ai_job_status", nullable: false),
                    category = table.Column<ClothingCategory>(type: "clothing_category", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("wardrobe_items_pkey", x => x.internal_id);
                    table.ForeignKey(
                        name: "wardrobe_items_user_internal_id_fkey",
                        column: x => x.user_internal_id,
                        principalTable: "users",
                        principalColumn: "internal_id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "T? d? s?. M?i item c� ?nh g?c v� ?nh d� x�a n?n d? gh�p canvas/mannequin.");

            migrationBuilder.CreateTable(
                name: "sponsored_campaigns",
                columns: table => new
                {
                    internal_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    brand_internal_id = table.Column<int>(type: "integer", nullable: false),
                    affiliate_product_internal_id = table.Column<int>(type: "integer", nullable: false),
                    display_rank = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)99),
                    daily_budget = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    total_spent = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    impression_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    click_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    start_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("sponsored_campaigns_pkey", x => x.internal_id);
                    table.ForeignKey(
                        name: "sponsored_campaigns_affiliate_product_internal_id_fkey",
                        column: x => x.affiliate_product_internal_id,
                        principalTable: "affiliate_products",
                        principalColumn: "internal_id");
                    table.ForeignKey(
                        name: "sponsored_campaigns_brand_internal_id_fkey",
                        column: x => x.brand_internal_id,
                        principalTable: "brand_profiles",
                        principalColumn: "internal_id");
                },
                comment: "Campaign qu?ng c�o brand partner. display_rank quy?t d?nh th? t? Tab Kh�m Ph�.");

            migrationBuilder.CreateTable(
                name: "affiliate_clicks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_internal_id = table.Column<int>(type: "integer", nullable: true),
                    affiliate_product_internal_id = table.Column<int>(type: "integer", nullable: false),
                    outfit_internal_id = table.Column<int>(type: "integer", nullable: true),
                    click_source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValueSql: "'discovery'::character varying"),
                    ip_address = table.Column<IPAddress>(type: "inet", nullable: true),
                    user_agent = table.Column<string>(type: "text", nullable: true),
                    clicked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("affiliate_clicks_pkey", x => x.id);
                    table.ForeignKey(
                        name: "affiliate_clicks_affiliate_product_internal_id_fkey",
                        column: x => x.affiliate_product_internal_id,
                        principalTable: "affiliate_products",
                        principalColumn: "internal_id");
                    table.ForeignKey(
                        name: "affiliate_clicks_outfit_internal_id_fkey",
                        column: x => x.outfit_internal_id,
                        principalTable: "canvas_outfits",
                        principalColumn: "internal_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "affiliate_clicks_user_internal_id_fkey",
                        column: x => x.user_internal_id,
                        principalTable: "users",
                        principalColumn: "internal_id",
                        onDelete: ReferentialAction.SetNull);
                },
                comment: "Log click affiliate. T�nh CTR, match conversion, ph�t hi?n click fraud.");

            migrationBuilder.CreateTable(
                name: "ai_lookbooks",
                columns: table => new
                {
                    internal_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    outfit_internal_id = table.Column<int>(type: "integer", nullable: false),
                    user_internal_id = table.Column<int>(type: "integer", nullable: false),
                    generated_image_url = table.Column<string>(type: "text", nullable: true),
                    ai_prompt_used = table.Column<string>(type: "text", nullable: true),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    generation_seconds = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    status = table.Column<AiJobStatus>(type: "ai_job_status", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ai_lookbooks_pkey", x => x.internal_id);
                    table.ForeignKey(
                        name: "ai_lookbooks_outfit_internal_id_fkey",
                        column: x => x.outfit_internal_id,
                        principalTable: "canvas_outfits",
                        principalColumn: "internal_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "ai_lookbooks_user_internal_id_fkey",
                        column: x => x.user_internal_id,
                        principalTable: "users",
                        principalColumn: "internal_id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "?nh lookbook AI generate t? canvas outfit. Luu prompt d? A/B test c?i thi?n model.");

            migrationBuilder.CreateTable(
                name: "community_posts",
                columns: table => new
                {
                    internal_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_internal_id = table.Column<int>(type: "integer", nullable: false),
                    outfit_internal_id = table.Column<int>(type: "integer", nullable: true),
                    caption = table.Column<string>(type: "text", nullable: true),
                    like_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    comment_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_public = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    is_hidden = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("community_posts_pkey", x => x.internal_id);
                    table.ForeignKey(
                        name: "community_posts_outfit_internal_id_fkey",
                        column: x => x.outfit_internal_id,
                        principalTable: "canvas_outfits",
                        principalColumn: "internal_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "community_posts_user_internal_id_fkey",
                        column: x => x.user_internal_id,
                        principalTable: "users",
                        principalColumn: "internal_id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "B�i dang community feed. G?n v?i canvas outfit d? ngu?i kh�c th? outfit tuong t?.");

            migrationBuilder.CreateTable(
                name: "chat_messages",
                columns: table => new
                {
                    internal_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    room_internal_id = table.Column<int>(type: "integer", nullable: false),
                    user_internal_id = table.Column<int>(type: "integer", nullable: false),
                    content = table.Column<string>(type: "text", nullable: true),
                    outfit_internal_id = table.Column<int>(type: "integer", nullable: true),
                    image_url = table.Column<string>(type: "text", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    message_type = table.Column<MessageType>(type: "message_type", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("chat_messages_pkey", x => x.internal_id);
                    table.ForeignKey(
                        name: "chat_messages_outfit_internal_id_fkey",
                        column: x => x.outfit_internal_id,
                        principalTable: "canvas_outfits",
                        principalColumn: "internal_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "chat_messages_room_internal_id_fkey",
                        column: x => x.room_internal_id,
                        principalTable: "chat_rooms",
                        principalColumn: "internal_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "chat_messages_user_internal_id_fkey",
                        column: x => x.user_internal_id,
                        principalTable: "users",
                        principalColumn: "internal_id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "L?ch s? tin nh?n. Share outfit v�o chat. Soft delete d? moderator ki?m duy?t.");

            migrationBuilder.CreateTable(
                name: "chat_room_members",
                columns: table => new
                {
                    room_internal_id = table.Column<int>(type: "integer", nullable: false),
                    user_internal_id = table.Column<int>(type: "integer", nullable: false),
                    joined_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    last_read_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_muted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("chat_room_members_pkey", x => new { x.room_internal_id, x.user_internal_id });
                    table.ForeignKey(
                        name: "chat_room_members_room_internal_id_fkey",
                        column: x => x.room_internal_id,
                        principalTable: "chat_rooms",
                        principalColumn: "internal_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "chat_room_members_user_internal_id_fkey",
                        column: x => x.user_internal_id,
                        principalTable: "users",
                        principalColumn: "internal_id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Th�nh vi�n ph�ng chat. last_read_at d�ng hi?n th? s? tin chua d?c.");

            migrationBuilder.CreateTable(
                name: "canvas_outfit_items",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    outfit_internal_id = table.Column<int>(type: "integer", nullable: false),
                    wardrobe_item_internal_id = table.Column<int>(type: "integer", nullable: true),
                    affiliate_product_internal_id = table.Column<int>(type: "integer", nullable: true),
                    pos_x = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    pos_y = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    scale = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: false, defaultValueSql: "1.0"),
                    rotation = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    z_index = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("canvas_outfit_items_pkey", x => x.id);
                    table.ForeignKey(
                        name: "canvas_outfit_items_affiliate_product_internal_id_fkey",
                        column: x => x.affiliate_product_internal_id,
                        principalTable: "affiliate_products",
                        principalColumn: "internal_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "canvas_outfit_items_outfit_internal_id_fkey",
                        column: x => x.outfit_internal_id,
                        principalTable: "canvas_outfits",
                        principalColumn: "internal_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "canvas_outfit_items_wardrobe_item_internal_id_fkey",
                        column: x => x.wardrobe_item_internal_id,
                        principalTable: "wardrobe_items",
                        principalColumn: "internal_id",
                        onDelete: ReferentialAction.SetNull);
                },
                comment: "V? tr� t?ng item tr�n canvas. �? nh� HO?C affiliate � CHECK constraint d?m b?o ch? 1 trong 2.");

            migrationBuilder.CreateTable(
                name: "campaign_impressions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    campaign_internal_id = table.Column<int>(type: "integer", nullable: false),
                    user_internal_id = table.Column<int>(type: "integer", nullable: true),
                    impressed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("campaign_impressions_pkey", x => x.id);
                    table.ForeignKey(
                        name: "campaign_impressions_campaign_internal_id_fkey",
                        column: x => x.campaign_internal_id,
                        principalTable: "sponsored_campaigns",
                        principalColumn: "internal_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "campaign_impressions_user_internal_id_fkey",
                        column: x => x.user_internal_id,
                        principalTable: "users",
                        principalColumn: "internal_id",
                        onDelete: ReferentialAction.SetNull);
                },
                comment: "Log impression sponsored. Volume cao � c�n nh?c partition theo th�ng khi scale.");

            migrationBuilder.CreateTable(
                name: "affiliate_conversions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    click_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_internal_id = table.Column<int>(type: "integer", nullable: true),
                    affiliate_product_internal_id = table.Column<int>(type: "integer", nullable: false),
                    shopee_order_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    order_amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    commission_rate = table.Column<decimal>(type: "numeric(4,3)", precision: 4, scale: 3, nullable: false),
                    commission_amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    converted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    confirmed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    paid_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<CommissionStatus>(type: "commission_status", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("affiliate_conversions_pkey", x => x.id);
                    table.ForeignKey(
                        name: "affiliate_conversions_affiliate_product_internal_id_fkey",
                        column: x => x.affiliate_product_internal_id,
                        principalTable: "affiliate_products",
                        principalColumn: "internal_id");
                    table.ForeignKey(
                        name: "affiliate_conversions_click_id_fkey",
                        column: x => x.click_id,
                        principalTable: "affiliate_clicks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "affiliate_conversions_user_internal_id_fkey",
                        column: x => x.user_internal_id,
                        principalTable: "users",
                        principalColumn: "internal_id",
                        onDelete: ReferentialAction.SetNull);
                },
                comment: "�on h�ng th�nh c�ng qua affiliate. commission_rate snapshot t?i th?i di?m chuy?n d?i.");

            migrationBuilder.CreateTable(
                name: "post_comments",
                columns: table => new
                {
                    internal_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    post_internal_id = table.Column<int>(type: "integer", nullable: false),
                    user_internal_id = table.Column<int>(type: "integer", nullable: false),
                    parent_comment_internal_id = table.Column<int>(type: "integer", nullable: true),
                    content = table.Column<string>(type: "text", nullable: false),
                    is_hidden = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("post_comments_pkey", x => x.internal_id);
                    table.ForeignKey(
                        name: "post_comments_parent_comment_internal_id_fkey",
                        column: x => x.parent_comment_internal_id,
                        principalTable: "post_comments",
                        principalColumn: "internal_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "post_comments_post_internal_id_fkey",
                        column: x => x.post_internal_id,
                        principalTable: "community_posts",
                        principalColumn: "internal_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "post_comments_user_internal_id_fkey",
                        column: x => x.user_internal_id,
                        principalTable: "users",
                        principalColumn: "internal_id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "B�nh lu?n b�i dang. H? tr? 1 c?p reply qua parent_comment_internal_id.");

            migrationBuilder.CreateTable(
                name: "post_likes",
                columns: table => new
                {
                    post_internal_id = table.Column<int>(type: "integer", nullable: false),
                    user_internal_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("post_likes_pkey", x => new { x.post_internal_id, x.user_internal_id });
                    table.ForeignKey(
                        name: "post_likes_post_internal_id_fkey",
                        column: x => x.post_internal_id,
                        principalTable: "community_posts",
                        principalColumn: "internal_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "post_likes_user_internal_id_fkey",
                        column: x => x.user_internal_id,
                        principalTable: "users",
                        principalColumn: "internal_id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Like b�i dang. Composite PK INT d?m b?o 1 user ch? like 1 b�i 1 l?n.");

            migrationBuilder.CreateTable(
                name: "post_reports",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    post_internal_id = table.Column<int>(type: "integer", nullable: false),
                    reporter_internal_id = table.Column<int>(type: "integer", nullable: false),
                    reason = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_resolved = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    resolved_by_internal = table.Column<int>(type: "integer", nullable: true),
                    resolved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("post_reports_pkey", x => x.id);
                    table.ForeignKey(
                        name: "post_reports_post_internal_id_fkey",
                        column: x => x.post_internal_id,
                        principalTable: "community_posts",
                        principalColumn: "internal_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "post_reports_reporter_internal_id_fkey",
                        column: x => x.reporter_internal_id,
                        principalTable: "users",
                        principalColumn: "internal_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "post_reports_resolved_by_internal_fkey",
                        column: x => x.resolved_by_internal,
                        principalTable: "users",
                        principalColumn: "internal_id");
                },
                comment: "Report vi ph?m. Moderator xem queue v� x? l� t?ng report.");

            migrationBuilder.CreateIndex(
                name: "idx_admin_permissions_user",
                table: "admin_permissions",
                column: "user_internal_id");

            migrationBuilder.CreateIndex(
                name: "IX_admin_permissions_granted_by_internal",
                table: "admin_permissions",
                column: "granted_by_internal");

            migrationBuilder.CreateIndex(
                name: "IX_admin_permissions_permission_id",
                table: "admin_permissions",
                column: "permission_id");

            migrationBuilder.CreateIndex(
                name: "admin_profiles_id_key",
                table: "admin_profiles",
                column: "id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "admin_profiles_user_internal_id_key",
                table: "admin_profiles",
                column: "user_internal_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_admin_profiles_permission_level",
                table: "admin_profiles",
                column: "permission_level");

            migrationBuilder.CreateIndex(
                name: "idx_clicks_product",
                table: "affiliate_clicks",
                columns: new[] { "affiliate_product_internal_id", "clicked_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "idx_clicks_user",
                table: "affiliate_clicks",
                columns: new[] { "user_internal_id", "clicked_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_affiliate_clicks_outfit_internal_id",
                table: "affiliate_clicks",
                column: "outfit_internal_id");

            migrationBuilder.CreateIndex(
                name: "idx_conversions_product",
                table: "affiliate_conversions",
                column: "affiliate_product_internal_id");

            migrationBuilder.CreateIndex(
                name: "idx_conversions_user",
                table: "affiliate_conversions",
                column: "user_internal_id");

            migrationBuilder.CreateIndex(
                name: "IX_affiliate_conversions_click_id",
                table: "affiliate_conversions",
                column: "click_id");

            migrationBuilder.CreateIndex(
                name: "affiliate_products_id_key",
                table: "affiliate_products",
                column: "id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "affiliate_products_shopee_product_id_key",
                table: "affiliate_products",
                column: "shopee_product_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "affiliate_products_tracking_code_key",
                table: "affiliate_products",
                column: "tracking_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_affiliate_tracking",
                table: "affiliate_products",
                column: "tracking_code");

            migrationBuilder.CreateIndex(
                name: "idx_affiliate_trending",
                table: "affiliate_products",
                columns: new[] { "is_trending", "is_active" });

            migrationBuilder.CreateIndex(
                name: "idx_affiliate_uuid",
                table: "affiliate_products",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "ai_lookbooks_id_key",
                table: "ai_lookbooks",
                column: "id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_lookbooks_outfit",
                table: "ai_lookbooks",
                column: "outfit_internal_id");

            migrationBuilder.CreateIndex(
                name: "idx_lookbooks_user",
                table: "ai_lookbooks",
                column: "user_internal_id");

            migrationBuilder.CreateIndex(
                name: "idx_lookbooks_uuid",
                table: "ai_lookbooks",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "brand_profiles_id_key",
                table: "brand_profiles",
                column: "id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "brand_profiles_user_internal_id_key",
                table: "brand_profiles",
                column: "user_internal_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_impressions_campaign",
                table: "campaign_impressions",
                columns: new[] { "campaign_internal_id", "impressed_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_campaign_impressions_user_internal_id",
                table: "campaign_impressions",
                column: "user_internal_id");

            migrationBuilder.CreateIndex(
                name: "idx_canvas_items_outfit",
                table: "canvas_outfit_items",
                column: "outfit_internal_id");

            migrationBuilder.CreateIndex(
                name: "IX_canvas_outfit_items_affiliate_product_internal_id",
                table: "canvas_outfit_items",
                column: "affiliate_product_internal_id");

            migrationBuilder.CreateIndex(
                name: "IX_canvas_outfit_items_wardrobe_item_internal_id",
                table: "canvas_outfit_items",
                column: "wardrobe_item_internal_id");

            migrationBuilder.CreateIndex(
                name: "canvas_outfits_id_key",
                table: "canvas_outfits",
                column: "id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_canvas_outfits_public",
                table: "canvas_outfits",
                columns: new[] { "is_public", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "idx_canvas_outfits_user",
                table: "canvas_outfits",
                column: "user_internal_id");

            migrationBuilder.CreateIndex(
                name: "idx_canvas_outfits_uuid",
                table: "canvas_outfits",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "chat_messages_id_key",
                table: "chat_messages",
                column: "id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_messages_room",
                table: "chat_messages",
                columns: new[] { "room_internal_id", "sent_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "idx_messages_user",
                table: "chat_messages",
                columns: new[] { "user_internal_id", "sent_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_chat_messages_outfit_internal_id",
                table: "chat_messages",
                column: "outfit_internal_id");

            migrationBuilder.CreateIndex(
                name: "idx_chat_members_user",
                table: "chat_room_members",
                column: "user_internal_id");

            migrationBuilder.CreateIndex(
                name: "chat_rooms_id_key",
                table: "chat_rooms",
                column: "id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_chat_rooms_uuid",
                table: "chat_rooms",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "IX_chat_rooms_created_by_internal",
                table: "chat_rooms",
                column: "created_by_internal");

            migrationBuilder.CreateIndex(
                name: "community_posts_id_key",
                table: "community_posts",
                column: "id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_posts_feed",
                table: "community_posts",
                columns: new[] { "is_public", "is_hidden", "created_at" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "idx_posts_outfit",
                table: "community_posts",
                column: "outfit_internal_id");

            migrationBuilder.CreateIndex(
                name: "idx_posts_user",
                table: "community_posts",
                columns: new[] { "user_internal_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "idx_posts_uuid",
                table: "community_posts",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "customer_profiles_id_key",
                table: "customer_profiles",
                column: "id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "customer_profiles_user_internal_id_key",
                table: "customer_profiles",
                column: "user_internal_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_notifications_unread",
                table: "notifications",
                column: "user_internal_id",
                filter: "(is_read = false)");

            migrationBuilder.CreateIndex(
                name: "idx_notifications_user",
                table: "notifications",
                columns: new[] { "user_internal_id", "is_read", "created_at" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "notifications_id_key",
                table: "notifications",
                column: "id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_permission_level_defaults_permission_id",
                table: "permission_level_defaults",
                column: "permission_id");

            migrationBuilder.CreateIndex(
                name: "permission_levels_name_key",
                table: "permission_levels",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "permissions_code_key",
                table: "permissions",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_comments_parent",
                table: "post_comments",
                column: "parent_comment_internal_id");

            migrationBuilder.CreateIndex(
                name: "idx_comments_post",
                table: "post_comments",
                columns: new[] { "post_internal_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_post_comments_user_internal_id",
                table: "post_comments",
                column: "user_internal_id");

            migrationBuilder.CreateIndex(
                name: "post_comments_id_key",
                table: "post_comments",
                column: "id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_post_likes_user_internal_id",
                table: "post_likes",
                column: "user_internal_id");

            migrationBuilder.CreateIndex(
                name: "idx_reports_unresolved",
                table: "post_reports",
                columns: new[] { "is_resolved", "created_at" },
                filter: "(is_resolved = false)");

            migrationBuilder.CreateIndex(
                name: "IX_post_reports_post_internal_id",
                table: "post_reports",
                column: "post_internal_id");

            migrationBuilder.CreateIndex(
                name: "IX_post_reports_reporter_internal_id",
                table: "post_reports",
                column: "reporter_internal_id");

            migrationBuilder.CreateIndex(
                name: "IX_post_reports_resolved_by_internal",
                table: "post_reports",
                column: "resolved_by_internal");

            migrationBuilder.CreateIndex(
                name: "idx_subscriptions_expiry",
                table: "premium_subscriptions",
                column: "expires_at",
                filter: "(is_active = true)");

            migrationBuilder.CreateIndex(
                name: "idx_subscriptions_user",
                table: "premium_subscriptions",
                columns: new[] { "user_internal_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "premium_subscriptions_id_key",
                table: "premium_subscriptions",
                column: "id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_refresh_tokens_token",
                table: "refresh_tokens",
                column: "token_hash");

            migrationBuilder.CreateIndex(
                name: "idx_refresh_tokens_user",
                table: "refresh_tokens",
                column: "user_internal_id");

            migrationBuilder.CreateIndex(
                name: "refresh_tokens_token_hash_key",
                table: "refresh_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_sponsored_active",
                table: "sponsored_campaigns",
                columns: new[] { "is_active", "start_at", "end_at" });

            migrationBuilder.CreateIndex(
                name: "idx_sponsored_brand",
                table: "sponsored_campaigns",
                column: "brand_internal_id");

            migrationBuilder.CreateIndex(
                name: "IX_sponsored_campaigns_affiliate_product_internal_id",
                table: "sponsored_campaigns",
                column: "affiliate_product_internal_id");

            migrationBuilder.CreateIndex(
                name: "sponsored_campaigns_id_key",
                table: "sponsored_campaigns",
                column: "id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_ban_logs_active",
                table: "user_ban_logs",
                columns: new[] { "user_internal_id", "ban_type" },
                filter: "(is_lifted = false)");

            migrationBuilder.CreateIndex(
                name: "idx_ban_logs_user",
                table: "user_ban_logs",
                columns: new[] { "user_internal_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_user_ban_logs_banned_by_internal",
                table: "user_ban_logs",
                column: "banned_by_internal");

            migrationBuilder.CreateIndex(
                name: "IX_user_ban_logs_lifted_by_internal",
                table: "user_ban_logs",
                column: "lifted_by_internal");

            migrationBuilder.CreateIndex(
                name: "idx_users_email",
                table: "users",
                column: "email");

            migrationBuilder.CreateIndex(
                name: "idx_users_google",
                table: "users",
                column: "google_id",
                filter: "(google_id IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "idx_users_uuid",
                table: "users",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "users_email_key",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "users_google_id_key",
                table: "users",
                column: "google_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "users_id_key",
                table: "users",
                column: "id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_wardrobe_active",
                table: "wardrobe_items",
                columns: new[] { "user_internal_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "idx_wardrobe_user",
                table: "wardrobe_items",
                column: "user_internal_id");

            migrationBuilder.CreateIndex(
                name: "idx_wardrobe_uuid",
                table: "wardrobe_items",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "wardrobe_items_id_key",
                table: "wardrobe_items",
                column: "id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "admin_permissions");

            migrationBuilder.DropTable(
                name: "admin_profiles");

            migrationBuilder.DropTable(
                name: "affiliate_conversions");

            migrationBuilder.DropTable(
                name: "ai_lookbooks");

            migrationBuilder.DropTable(
                name: "campaign_impressions");

            migrationBuilder.DropTable(
                name: "canvas_outfit_items");

            migrationBuilder.DropTable(
                name: "chat_messages");

            migrationBuilder.DropTable(
                name: "chat_room_members");

            migrationBuilder.DropTable(
                name: "customer_profiles");

            migrationBuilder.DropTable(
                name: "notifications");

            migrationBuilder.DropTable(
                name: "permission_level_defaults");

            migrationBuilder.DropTable(
                name: "post_comments");

            migrationBuilder.DropTable(
                name: "post_likes");

            migrationBuilder.DropTable(
                name: "post_reports");

            migrationBuilder.DropTable(
                name: "premium_subscriptions");

            migrationBuilder.DropTable(
                name: "refresh_tokens");

            migrationBuilder.DropTable(
                name: "user_ban_logs");

            migrationBuilder.DropTable(
                name: "affiliate_clicks");

            migrationBuilder.DropTable(
                name: "sponsored_campaigns");

            migrationBuilder.DropTable(
                name: "wardrobe_items");

            migrationBuilder.DropTable(
                name: "chat_rooms");

            migrationBuilder.DropTable(
                name: "permissions");

            migrationBuilder.DropTable(
                name: "permission_levels");

            migrationBuilder.DropTable(
                name: "community_posts");

            migrationBuilder.DropTable(
                name: "affiliate_products");

            migrationBuilder.DropTable(
                name: "brand_profiles");

            migrationBuilder.DropTable(
                name: "canvas_outfits");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
