using Npgsql;
using VCloset.Domain.Enums;
using Microsoft.Extensions.Configuration;
using System.IO;
using VCloset.Domain.Entities;
using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace VCloset.Infrastructure.Data;

public partial class VClosetVersion30Context : DbContext
{
    public VClosetVersion30Context()
    {
    }

    public VClosetVersion30Context(DbContextOptions<VClosetVersion30Context> options)
        : base(options)
    {
    }

    public virtual DbSet<AdminPermission> AdminPermissions { get; set; }

    public virtual DbSet<AdminProfile> AdminProfiles { get; set; }

    public virtual DbSet<AffiliateClick> AffiliateClicks { get; set; }

    public virtual DbSet<AffiliateConversion> AffiliateConversions { get; set; }

    public virtual DbSet<AffiliateProduct> AffiliateProducts { get; set; }

    public virtual DbSet<AiLookbook> AiLookbooks { get; set; }

    public virtual DbSet<BrandProfile> BrandProfiles { get; set; }

    public virtual DbSet<CampaignImpression> CampaignImpressions { get; set; }

    public virtual DbSet<CanvasOutfit> CanvasOutfits { get; set; }

    public virtual DbSet<CanvasOutfitItem> CanvasOutfitItems { get; set; }

    public virtual DbSet<ChatMessage> ChatMessages { get; set; }

    public virtual DbSet<ChatRoom> ChatRooms { get; set; }

    public virtual DbSet<ChatRoomMember> ChatRoomMembers { get; set; }

    public virtual DbSet<CommunityPost> CommunityPosts { get; set; }

    public virtual DbSet<CustomerProfile> CustomerProfiles { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<Permission> Permissions { get; set; }

    public virtual DbSet<PermissionLevel> PermissionLevels { get; set; }

    public virtual DbSet<PostComment> PostComments { get; set; }

    public virtual DbSet<UserFollower> UserFollowers { get; set; }

    public virtual DbSet<PostLike> PostLikes { get; set; }

    public virtual DbSet<PostReport> PostReports { get; set; }

    public virtual DbSet<PremiumSubscription> PremiumSubscriptions { get; set; }

    public virtual DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }

    public virtual DbSet<PaymentTransaction> PaymentTransactions { get; set; }

    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }

    public virtual DbSet<SponsoredCampaign> SponsoredCampaigns { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserBanLog> UserBanLogs { get; set; }

    public virtual DbSet<WardrobeItem> WardrobeItems { get; set; }

    public virtual DbSet<SubscriptionTierConfig> SubscriptionTierConfigs { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            dotenv.net.DotEnv.Load(options: new dotenv.net.DotEnvOptions(probeForEnv: true));
            
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env}.json", optional: true)
                .AddEnvironmentVariables()
                .Build();
                
            var connectionString = configuration.GetConnectionString("MyCnn");
            if (string.IsNullOrEmpty(connectionString) || connectionString.StartsWith("YOUR_CONNECTION_STRING") || connectionString.Contains("LOADED_FROM_ENV"))
            {
                connectionString = "Host=localhost;Database=dummy;Username=postgres;Password=postgres";
            }
            var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
            dataSourceBuilder.MapEnum<UserRole>("user_role")
                             .MapEnum<AuthProvider>("auth_provider")
                             .MapEnum<BodyShapeType>("body_shape_type")
                             .MapEnum<ClothingCategory>("clothing_category")
                             .MapEnum<AiJobStatus>("ai_job_status")
                             .MapEnum<CommissionStatus>("commission_status")
                             .MapEnum<PremiumPlan>("premium_plan")
                             .MapEnum<BrandStatus>("brand_status")
                             .MapEnum<ChatRoomType>("chat_room_type")
                             .MapEnum<MessageType>("message_type")
                             .MapEnum<PaymentStatus>("payment_status");
            var dataSource = dataSourceBuilder.Build();
            
            optionsBuilder.UseNpgsql(dataSource);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserFollower>(entity =>
        {
            entity.ToTable("user_followers");

            entity.HasOne(d => d.Follower)
                .WithMany()
                .HasForeignKey(d => d.FollowerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Following)
                .WithMany()
                .HasForeignKey(d => d.FollowingId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder
            .HasPostgresEnum("ai_job_status", new[] { "pending", "processing", "completed", "failed" })
            .HasPostgresEnum("auth_provider", new[] { "local", "google" })
            .HasPostgresEnum("body_shape_type", new[] { "hourglass", "pear", "apple", "rectangle", "inverted_triangle" })
            .HasPostgresEnum("brand_status", new[] { "pending", "verified", "suspended" })
            .HasPostgresEnum("chat_room_type", new[] { "public", "topic", "direct" })
            .HasPostgresEnum("clothing_category", new[] { "top", "bottom", "dress", "outerwear", "shoes", "bag", "accessory", "other" })
            .HasPostgresEnum("commission_status", new[] { "pending", "confirmed", "paid", "rejected" })
            .HasPostgresEnum("message_type", new[] { "text", "image", "outfit_share", "system" })
            .HasPostgresEnum("payment_status", new[] { "pending", "success", "failed", "cancelled", "expired" })
            .HasPostgresEnum("premium_plan", new[] { "monthly", "yearly" })
            .HasPostgresEnum("user_role", new[] { "customer", "admin", "moderator", "brand_partner" })
            .HasPostgresExtension("pgcrypto");

        modelBuilder.Entity<AdminPermission>(entity =>
        {
            entity.HasKey(e => new { e.UserInternalId, e.PermissionId }).HasName("admin_permissions_pkey");

            entity.ToTable("admin_permissions", tb => tb.HasComment("Permission c? th? t?ng admin. Composite PK INT. granted_by_internal l� audit trail."));

            entity.HasIndex(e => e.UserInternalId, "idx_admin_permissions_user");

            entity.Property(e => e.UserInternalId).HasColumnName("user_internal_id");
            entity.Property(e => e.PermissionId).HasColumnName("permission_id");
            entity.Property(e => e.GrantedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("granted_at");
            entity.Property(e => e.GrantedByInternal).HasColumnName("granted_by_internal");

            entity.HasOne(d => d.GrantedByInternalNavigation).WithMany(p => p.AdminPermissionGrantedByInternalNavigations)
                .HasForeignKey(d => d.GrantedByInternal)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("admin_permissions_granted_by_internal_fkey");

            entity.HasOne(d => d.Permission).WithMany(p => p.AdminPermissions)
                .HasForeignKey(d => d.PermissionId)
                .HasConstraintName("admin_permissions_permission_id_fkey");

            entity.HasOne(d => d.UserInternal).WithMany(p => p.AdminPermissionUserInternals)
                .HasForeignKey(d => d.UserInternalId)
                .HasConstraintName("admin_permissions_user_internal_id_fkey");
        });

        modelBuilder.Entity<AdminProfile>(entity =>
        {
            entity.HasKey(e => e.InternalId).HasName("admin_profiles_pkey");

            entity.ToTable("admin_profiles", tb => tb.HasComment("Profile admin/moderator. permission_level l� vai tr� t?ng th?, chi ti?t ? admin_permissions."));

            entity.HasIndex(e => e.Id, "admin_profiles_id_key").IsUnique();

            entity.HasIndex(e => e.UserInternalId, "admin_profiles_user_internal_id_key").IsUnique();

            entity.Property(e => e.InternalId).HasColumnName("internal_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Department)
                .HasMaxLength(100)
                .HasColumnName("department");
            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.PermissionLevel)
                .HasDefaultValue((short)1)
                .HasColumnName("permission_level");
            entity.Property(e => e.UserInternalId).HasColumnName("user_internal_id");

            entity.HasOne(d => d.PermissionLevelNavigation).WithMany(p => p.AdminProfiles)
                .HasForeignKey(d => d.PermissionLevel)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("admin_profiles_permission_level_fkey");

            entity.HasOne(d => d.UserInternal).WithOne(p => p.AdminProfile)
                .HasForeignKey<AdminProfile>(d => d.UserInternalId)
                .HasConstraintName("admin_profiles_user_internal_id_fkey");
        });

        modelBuilder.Entity<AffiliateClick>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("affiliate_clicks_pkey");

            entity.ToTable("affiliate_clicks", tb => tb.HasComment("Log click affiliate. T�nh CTR, match conversion, ph�t hi?n click fraud."));

            entity.HasIndex(e => new { e.AffiliateProductInternalId, e.ClickedAt }, "idx_clicks_product").IsDescending(false, true);

            entity.HasIndex(e => new { e.UserInternalId, e.ClickedAt }, "idx_clicks_user").IsDescending(false, true);

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.AffiliateProductInternalId).HasColumnName("affiliate_product_internal_id");
            entity.Property(e => e.ClickSource)
                .HasMaxLength(50)
                .HasDefaultValueSql("'discovery'::character varying")
                .HasColumnName("click_source");
            entity.Property(e => e.ClickedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("clicked_at");
            entity.Property(e => e.IpAddress).HasColumnName("ip_address");
            entity.Property(e => e.OutfitInternalId).HasColumnName("outfit_internal_id");
            entity.Property(e => e.UserAgent).HasColumnName("user_agent");
            entity.Property(e => e.UserInternalId).HasColumnName("user_internal_id");

            entity.HasOne(d => d.AffiliateProductInternal).WithMany(p => p.AffiliateClicks)
                .HasForeignKey(d => d.AffiliateProductInternalId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("affiliate_clicks_affiliate_product_internal_id_fkey");

            entity.HasOne(d => d.OutfitInternal).WithMany(p => p.AffiliateClicks)
                .HasForeignKey(d => d.OutfitInternalId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("affiliate_clicks_outfit_internal_id_fkey");

            entity.HasOne(d => d.UserInternal).WithMany(p => p.AffiliateClicks)
                .HasForeignKey(d => d.UserInternalId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("affiliate_clicks_user_internal_id_fkey");
        });

        modelBuilder.Entity<AffiliateConversion>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("affiliate_conversions_pkey");

            entity.ToTable("affiliate_conversions", tb => tb.HasComment("�on h�ng th�nh c�ng qua affiliate. commission_rate snapshot t?i th?i di?m chuy?n d?i."));

            entity.HasIndex(e => e.AffiliateProductInternalId, "idx_conversions_product");

            entity.HasIndex(e => e.UserInternalId, "idx_conversions_user");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.AffiliateProductInternalId).HasColumnName("affiliate_product_internal_id");
            entity.Property(e => e.ClickId).HasColumnName("click_id");
            entity.Property(e => e.CommissionAmount)
                .HasPrecision(12, 2)
                .HasColumnName("commission_amount");
            entity.Property(e => e.CommissionRate)
                .HasPrecision(4, 3)
                .HasColumnName("commission_rate");
            entity.Property(e => e.ConfirmedAt).HasColumnName("confirmed_at");
            entity.Property(e => e.ConvertedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("converted_at");
            entity.Property(e => e.OrderAmount)
                .HasPrecision(12, 2)
                .HasColumnName("order_amount");
            entity.Property(e => e.PaidAt).HasColumnName("paid_at");
            entity.Property(e => e.ShopeeOrderId)
                .HasMaxLength(100)
                .HasColumnName("shopee_order_id");
            entity.Property(e => e.UserInternalId).HasColumnName("user_internal_id");

            entity.HasOne(d => d.AffiliateProductInternal).WithMany(p => p.AffiliateConversions)
                .HasForeignKey(d => d.AffiliateProductInternalId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("affiliate_conversions_affiliate_product_internal_id_fkey");

            entity.HasOne(d => d.Click).WithMany(p => p.AffiliateConversions)
                .HasForeignKey(d => d.ClickId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("affiliate_conversions_click_id_fkey");

            entity.HasOne(d => d.UserInternal).WithMany(p => p.AffiliateConversions)
                .HasForeignKey(d => d.UserInternalId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("affiliate_conversions_user_internal_id_fkey");
        });

        modelBuilder.Entity<AffiliateProduct>(entity =>
        {
            entity.HasKey(e => e.InternalId).HasName("affiliate_products_pkey");

            entity.ToTable("affiliate_products", tb => tb.HasComment("S?n ph?m trending sync t? Shopee m?i d�m. T?o tru?c canvas_outfit_items v� c� FK ph? thu?c."));

            entity.HasIndex(e => e.Id, "affiliate_products_id_key").IsUnique();

            entity.HasIndex(e => e.ShopeeProductId, "affiliate_products_shopee_product_id_key").IsUnique();

            entity.HasIndex(e => e.TrackingCode, "affiliate_products_tracking_code_key").IsUnique();

            entity.HasIndex(e => e.TrackingCode, "idx_affiliate_tracking");

            entity.HasIndex(e => new { e.IsTrending, e.IsActive }, "idx_affiliate_trending");

            entity.HasIndex(e => e.Id, "idx_affiliate_uuid");

            entity.Property(e => e.InternalId).HasColumnName("internal_id");
            entity.Property(e => e.AffiliateLink).HasColumnName("affiliate_link");
            entity.Property(e => e.ClickCount)
                .HasDefaultValue(0)
                .HasColumnName("click_count");
            entity.Property(e => e.ConversionCount)
                .HasDefaultValue(0)
                .HasColumnName("conversion_count");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.ImageUrl).HasColumnName("image_url");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.IsTrending)
                .HasDefaultValue(false)
                .HasColumnName("is_trending");
            entity.Property(e => e.Name)
                .HasMaxLength(500)
                .HasColumnName("name");
            entity.Property(e => e.OriginalPrice)
                .HasPrecision(12, 2)
                .HasColumnName("original_price");
            entity.Property(e => e.Price)
                .HasPrecision(12, 2)
                .HasColumnName("price");
            entity.Property(e => e.ShopeeProductId)
                .HasMaxLength(100)
                .HasColumnName("shopee_product_id");
            entity.Property(e => e.ShopeeShopId)
                .HasMaxLength(100)
                .HasColumnName("shopee_shop_id");
            entity.Property(e => e.SyncedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("synced_at");
            entity.Property(e => e.TrackingCode)
                .HasMaxLength(100)
                .HasColumnName("tracking_code");
        });

        modelBuilder.Entity<AiLookbook>(entity =>
        {
            entity.HasKey(e => e.InternalId).HasName("ai_lookbooks_pkey");

            entity.ToTable("ai_lookbooks", tb => tb.HasComment("?nh lookbook AI generate t? canvas outfit. Luu prompt d? A/B test c?i thi?n model."));

            entity.HasIndex(e => e.Id, "ai_lookbooks_id_key").IsUnique();

            entity.HasIndex(e => e.OutfitInternalId, "idx_lookbooks_outfit");

            entity.HasIndex(e => e.UserInternalId, "idx_lookbooks_user");

            entity.HasIndex(e => e.Id, "idx_lookbooks_uuid");

            entity.Property(e => e.InternalId).HasColumnName("internal_id");
            entity.Property(e => e.AiPromptUsed).HasColumnName("ai_prompt_used");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.ErrorMessage).HasColumnName("error_message");
            entity.Property(e => e.GeneratedImageUrl).HasColumnName("generated_image_url");
            entity.Property(e => e.GenerationSeconds)
                .HasPrecision(6, 2)
                .HasColumnName("generation_seconds");
            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.OutfitInternalId).HasColumnName("outfit_internal_id");
            entity.Property(e => e.UserInternalId).HasColumnName("user_internal_id");

            entity.HasOne(d => d.OutfitInternal).WithMany(p => p.AiLookbooks)
                .HasForeignKey(d => d.OutfitInternalId)
                .HasConstraintName("ai_lookbooks_outfit_internal_id_fkey");

            entity.HasOne(d => d.UserInternal).WithMany(p => p.AiLookbooks)
                .HasForeignKey(d => d.UserInternalId)
                .HasConstraintName("ai_lookbooks_user_internal_id_fkey");
        });

        modelBuilder.Entity<BrandProfile>(entity =>
        {
            entity.HasKey(e => e.InternalId).HasName("brand_profiles_pkey");

            entity.ToTable("brand_profiles", tb => tb.HasComment("Profile brand partner B2B. Admin verify tru?c khi ch?y sponsored campaign."));

            entity.HasIndex(e => e.Id, "brand_profiles_id_key").IsUnique();

            entity.HasIndex(e => e.UserInternalId, "brand_profiles_user_internal_id_key").IsUnique();

            entity.Property(e => e.InternalId).HasColumnName("internal_id");
            entity.Property(e => e.BrandName)
                .HasMaxLength(255)
                .HasColumnName("brand_name");
            entity.Property(e => e.ContactPhone)
                .HasMaxLength(20)
                .HasColumnName("contact_phone");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreditBalance)
                .HasPrecision(12, 2)
                .HasColumnName("credit_balance");
            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.LogoUrl).HasColumnName("logo_url");
            entity.Property(e => e.TaxCode)
                .HasMaxLength(50)
                .HasColumnName("tax_code");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserInternalId).HasColumnName("user_internal_id");
            entity.Property(e => e.WebsiteUrl).HasColumnName("website_url");

            entity.HasOne(d => d.UserInternal).WithOne(p => p.BrandProfile)
                .HasForeignKey<BrandProfile>(d => d.UserInternalId)
                .HasConstraintName("brand_profiles_user_internal_id_fkey");
        });

        modelBuilder.Entity<CampaignImpression>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("campaign_impressions_pkey");

            entity.ToTable("campaign_impressions", tb => tb.HasComment("Log impression sponsored. Volume cao � c�n nh?c partition theo th�ng khi scale."));

            entity.HasIndex(e => new { e.CampaignInternalId, e.ImpressedAt }, "idx_impressions_campaign").IsDescending(false, true);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CampaignInternalId).HasColumnName("campaign_internal_id");
            entity.Property(e => e.ImpressedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("impressed_at");
            entity.Property(e => e.UserInternalId).HasColumnName("user_internal_id");

            entity.HasOne(d => d.CampaignInternal).WithMany(p => p.CampaignImpressions)
                .HasForeignKey(d => d.CampaignInternalId)
                .HasConstraintName("campaign_impressions_campaign_internal_id_fkey");

            entity.HasOne(d => d.UserInternal).WithMany(p => p.CampaignImpressions)
                .HasForeignKey(d => d.UserInternalId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("campaign_impressions_user_internal_id_fkey");
        });

        modelBuilder.Entity<CanvasOutfit>(entity =>
        {
            entity.HasKey(e => e.InternalId).HasName("canvas_outfits_pkey");

            entity.ToTable("canvas_outfits", tb => tb.HasComment("Outfit t?o t? Canvas 2D. Ch?a d? t? t? nh� v� d? trending affiliate."));

            entity.HasIndex(e => e.Id, "canvas_outfits_id_key").IsUnique();

            entity.HasIndex(e => new { e.IsPublic, e.CreatedAt }, "idx_canvas_outfits_public").IsDescending(false, true);

            entity.HasIndex(e => e.UserInternalId, "idx_canvas_outfits_user");

            entity.HasIndex(e => e.Id, "idx_canvas_outfits_uuid");

            entity.Property(e => e.InternalId).HasColumnName("internal_id");
            entity.Property(e => e.CanvasSnapshotUrl).HasColumnName("canvas_snapshot_url");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.IsPublic)
                .HasDefaultValue(false)
                .HasColumnName("is_public");
            entity.Property(e => e.LikeCount)
                .HasDefaultValue(0)
                .HasColumnName("like_count");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .HasColumnName("title");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserInternalId).HasColumnName("user_internal_id");

            entity.HasOne(d => d.UserInternal).WithMany(p => p.CanvasOutfits)
                .HasForeignKey(d => d.UserInternalId)
                .HasConstraintName("canvas_outfits_user_internal_id_fkey");
        });

        modelBuilder.Entity<CanvasOutfitItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("canvas_outfit_items_pkey");

            entity.ToTable("canvas_outfit_items", tb => tb.HasComment("V? tr� t?ng item tr�n canvas. �? nh� HO?C affiliate � CHECK constraint d?m b?o ch? 1 trong 2."));

            entity.HasIndex(e => e.OutfitInternalId, "idx_canvas_items_outfit");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AffiliateProductInternalId).HasColumnName("affiliate_product_internal_id");
            entity.Property(e => e.OutfitInternalId).HasColumnName("outfit_internal_id");
            entity.Property(e => e.PosX)
                .HasPrecision(8, 2)
                .HasColumnName("pos_x");
            entity.Property(e => e.PosY)
                .HasPrecision(8, 2)
                .HasColumnName("pos_y");
            entity.Property(e => e.Rotation)
                .HasPrecision(6, 2)
                .HasColumnName("rotation");
            entity.Property(e => e.Scale)
                .HasPrecision(4, 2)
                .HasDefaultValueSql("1.0")
                .HasColumnName("scale");
            entity.Property(e => e.WardrobeItemInternalId).HasColumnName("wardrobe_item_internal_id");
            entity.Property(e => e.ZIndex)
                .HasDefaultValue((short)0)
                .HasColumnName("z_index");

            entity.HasOne(d => d.AffiliateProductInternal).WithMany(p => p.CanvasOutfitItems)
                .HasForeignKey(d => d.AffiliateProductInternalId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("canvas_outfit_items_affiliate_product_internal_id_fkey");

            entity.HasOne(d => d.OutfitInternal).WithMany(p => p.CanvasOutfitItems)
                .HasForeignKey(d => d.OutfitInternalId)
                .HasConstraintName("canvas_outfit_items_outfit_internal_id_fkey");

            entity.HasOne(d => d.WardrobeItemInternal).WithMany(p => p.CanvasOutfitItems)
                .HasForeignKey(d => d.WardrobeItemInternalId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("canvas_outfit_items_wardrobe_item_internal_id_fkey");
        });

        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.HasKey(e => e.InternalId).HasName("chat_messages_pkey");

            entity.ToTable("chat_messages", tb => tb.HasComment("L?ch s? tin nh?n. Share outfit v�o chat. Soft delete d? moderator ki?m duy?t."));

            entity.HasIndex(e => e.Id, "chat_messages_id_key").IsUnique();

            entity.HasIndex(e => new { e.RoomInternalId, e.SentAt }, "idx_messages_room").IsDescending(false, true);

            entity.HasIndex(e => new { e.UserInternalId, e.SentAt }, "idx_messages_user").IsDescending(false, true);

            entity.Property(e => e.InternalId).HasColumnName("internal_id");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.ImageUrl).HasColumnName("image_url");
            entity.Property(e => e.OutfitInternalId).HasColumnName("outfit_internal_id");
            entity.Property(e => e.RoomInternalId).HasColumnName("room_internal_id");
            entity.Property(e => e.SentAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("sent_at");
            entity.Property(e => e.UserInternalId).HasColumnName("user_internal_id");

            entity.HasOne(d => d.OutfitInternal).WithMany(p => p.ChatMessages)
                .HasForeignKey(d => d.OutfitInternalId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("chat_messages_outfit_internal_id_fkey");

            entity.HasOne(d => d.RoomInternal).WithMany(p => p.ChatMessages)
                .HasForeignKey(d => d.RoomInternalId)
                .HasConstraintName("chat_messages_room_internal_id_fkey");

            entity.HasOne(d => d.UserInternal).WithMany(p => p.ChatMessages)
                .HasForeignKey(d => d.UserInternalId)
                .HasConstraintName("chat_messages_user_internal_id_fkey");
        });

        modelBuilder.Entity<ChatRoom>(entity =>
        {
            entity.HasKey(e => e.InternalId).HasName("chat_rooms_pkey");

            entity.ToTable("chat_rooms", tb => tb.HasComment("Ph�ng chat: public, topic (theo ch? d? th?i trang), direct (2 ngu?i)."));

            entity.HasIndex(e => e.Id, "chat_rooms_id_key").IsUnique();

            entity.HasIndex(e => e.Id, "idx_chat_rooms_uuid");

            entity.Property(e => e.InternalId).HasColumnName("internal_id");
            entity.Property(e => e.CoverUrl).HasColumnName("cover_url");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedByInternal).HasColumnName("created_by_internal");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");

            entity.HasOne(d => d.CreatedByInternalNavigation).WithMany(p => p.ChatRooms)
                .HasForeignKey(d => d.CreatedByInternal)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("chat_rooms_created_by_internal_fkey");
        });

        modelBuilder.Entity<ChatRoomMember>(entity =>
        {
            entity.HasKey(e => new { e.RoomInternalId, e.UserInternalId }).HasName("chat_room_members_pkey");

            entity.ToTable("chat_room_members", tb => tb.HasComment("Th�nh vi�n ph�ng chat. last_read_at d�ng hi?n th? s? tin chua d?c."));

            entity.HasIndex(e => e.UserInternalId, "idx_chat_members_user");

            entity.Property(e => e.RoomInternalId).HasColumnName("room_internal_id");
            entity.Property(e => e.UserInternalId).HasColumnName("user_internal_id");
            entity.Property(e => e.IsMuted)
                .HasDefaultValue(false)
                .HasColumnName("is_muted");
            entity.Property(e => e.JoinedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("joined_at");
            entity.Property(e => e.LastReadAt).HasColumnName("last_read_at");

            entity.HasOne(d => d.RoomInternal).WithMany(p => p.ChatRoomMembers)
                .HasForeignKey(d => d.RoomInternalId)
                .HasConstraintName("chat_room_members_room_internal_id_fkey");

            entity.HasOne(d => d.UserInternal).WithMany(p => p.ChatRoomMembers)
                .HasForeignKey(d => d.UserInternalId)
                .HasConstraintName("chat_room_members_user_internal_id_fkey");
        });

        modelBuilder.Entity<CommunityPost>(entity =>
        {
            entity.HasKey(e => e.InternalId).HasName("community_posts_pkey");

            entity.ToTable("community_posts", tb => tb.HasComment("B�i dang community feed. G?n v?i canvas outfit d? ngu?i kh�c th? outfit tuong t?."));

            entity.HasIndex(e => e.Id, "community_posts_id_key").IsUnique();

            entity.HasIndex(e => new { e.IsPublic, e.IsHidden, e.CreatedAt }, "idx_posts_feed").IsDescending(false, false, true);

            entity.HasIndex(e => e.OutfitInternalId, "idx_posts_outfit");

            entity.HasIndex(e => new { e.UserInternalId, e.CreatedAt }, "idx_posts_user").IsDescending(false, true);

            entity.HasIndex(e => e.Id, "idx_posts_uuid");

            entity.Property(e => e.InternalId).HasColumnName("internal_id");
            entity.Property(e => e.Caption).HasColumnName("caption");
            entity.Property(e => e.CommentCount)
                .HasDefaultValue(0)
                .HasColumnName("comment_count");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.IsHidden)
                .HasDefaultValue(false)
                .HasColumnName("is_hidden");
            entity.Property(e => e.IsPublic)
                .HasDefaultValue(true)
                .HasColumnName("is_public");
            entity.Property(e => e.LikeCount)
                .HasDefaultValue(0)
                .HasColumnName("like_count");
            entity.Property(e => e.OutfitInternalId).HasColumnName("outfit_internal_id");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserInternalId).HasColumnName("user_internal_id");

            entity.HasOne(d => d.OutfitInternal).WithMany(p => p.CommunityPosts)
                .HasForeignKey(d => d.OutfitInternalId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("community_posts_outfit_internal_id_fkey");

            entity.HasOne(d => d.UserInternal).WithMany(p => p.CommunityPosts)
                .HasForeignKey(d => d.UserInternalId)
                .HasConstraintName("community_posts_user_internal_id_fkey");
        });

        modelBuilder.Entity<CustomerProfile>(entity =>
        {
            entity.HasKey(e => e.InternalId).HasName("customer_profiles_pkey");

            entity.ToTable("customer_profiles", tb => tb.HasComment("Profile customer: s? do, mannequin AI, tr?ng th�i ban. FK d�ng INT."));

            entity.HasIndex(e => e.Id, "customer_profiles_id_key").IsUnique();

            entity.HasIndex(e => e.UserInternalId, "customer_profiles_user_internal_id_key").IsUnique();

            entity.Property(e => e.InternalId).HasColumnName("internal_id");
            entity.Property(e => e.ChatBannedUntil).HasColumnName("chat_banned_until");
            entity.Property(e => e.HeightCm)
                .HasPrecision(5, 2)
                .HasColumnName("height_cm");
            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.IsChatBanned)
                .HasDefaultValue(false)
                .HasComment("TRUE = b? kho� chat. K?t h?p chat_banned_until ph�n bi?t t?m th?i/vinh vi?n.")
                .HasColumnName("is_chat_banned");
            entity.Property(e => e.IsPostBanned)
                .HasDefaultValue(false)
                .HasComment("TRUE = b? kho� dang b�i. K?t h?p post_banned_until ph�n bi?t t?m th?i/vinh vi?n.")
                .HasColumnName("is_post_banned");
            entity.Property(e => e.MannequinGeneratedAt).HasColumnName("mannequin_generated_at");
            entity.Property(e => e.MannequinImageUrl).HasColumnName("mannequin_image_url");
            entity.Property(e => e.PostBannedUntil).HasColumnName("post_banned_until");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserInternalId).HasColumnName("user_internal_id");
            entity.Property(e => e.WardrobeItemCount)
                .HasDefaultValue(0)
                .HasComment("Cache d? check gi?i h?n freemium 50 items m khng COUNT(*).")
                .HasColumnName("wardrobe_item_count");
            entity.Property(e => e.BgRemovalCredits)
                .HasDefaultValue(0)
                .HasColumnName("bg_removal_credits");
            entity.Property(e => e.TryOnCredits)
                .HasDefaultValue(0)
                .HasColumnName("try_on_credits");
            entity.Property(e => e.WeightKg)
                .HasPrecision(5, 2)
                .HasColumnName("weight_kg");
            entity.Property(e => e.DateOfBirth).HasColumnName("date_of_birth");
            entity.Property(e => e.PhoneNumber).HasMaxLength(20).HasColumnName("phone_number");
            entity.Property(e => e.Address).HasMaxLength(500).HasColumnName("address");
            entity.Property(e => e.Gender).HasMaxLength(20).HasColumnName("gender");
            entity.Property(e => e.Country).HasMaxLength(100).HasColumnName("country");
            entity.Property(e => e.IsOnboardingCompleted).HasColumnName("is_onboarding_completed").HasDefaultValue(false);
            entity.Property(e => e.Lifestyle).HasMaxLength(200).HasColumnName("lifestyle");
            entity.Property(e => e.EyeColor).HasMaxLength(50).HasColumnName("eye_color");
            entity.Property(e => e.Hair).HasMaxLength(100).HasColumnName("hair");

            entity.HasOne(d => d.UserInternal).WithOne(p => p.CustomerProfile)
                .HasForeignKey<CustomerProfile>(d => d.UserInternalId)
                .HasConstraintName("customer_profiles_user_internal_id_fkey");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.InternalId).HasName("notifications_pkey");

            entity.ToTable("notifications", tb => tb.HasComment("Th�ng b�o in-app. reference_id l� internal_id c?a object li�n quan."));

            entity.HasIndex(e => e.UserInternalId, "idx_notifications_unread").HasFilter("(is_read = false)");

            entity.HasIndex(e => new { e.UserInternalId, e.IsRead, e.CreatedAt }, "idx_notifications_user").IsDescending(false, false, true);

            entity.HasIndex(e => e.Id, "notifications_id_key").IsUnique();

            entity.Property(e => e.InternalId).HasColumnName("internal_id");
            entity.Property(e => e.Body).HasColumnName("body");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.IsRead)
                .HasDefaultValue(false)
                .HasColumnName("is_read");
            entity.Property(e => e.ReferenceId).HasColumnName("reference_id");
            entity.Property(e => e.ReferenceType)
                .HasMaxLength(50)
                .HasColumnName("reference_type");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .HasColumnName("title");
            entity.Property(e => e.Type)
                .HasMaxLength(50)
                .HasColumnName("type");
            entity.Property(e => e.UserInternalId).HasColumnName("user_internal_id");

            entity.HasOne(d => d.UserInternal).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.UserInternalId)
                .HasConstraintName("notifications_user_internal_id_fkey");
        });

        modelBuilder.Entity<Permission>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("permissions_pkey");

            entity.ToTable("permissions", tb => tb.HasComment("Danh m?c permission. code d?ng group.action d�ng trong C# RequirePermission attribute."));

            entity.HasIndex(e => e.Code, "permissions_code_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code)
                .HasMaxLength(100)
                .HasColumnName("code");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Grp)
                .HasMaxLength(50)
                .HasColumnName("grp");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
        });

        modelBuilder.Entity<PermissionLevel>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("permission_levels_pkey");

            entity.ToTable("permission_levels", tb => tb.HasComment("C?p quy?n t?ng th? cho admin/moderator. FK t? admin_profiles."));

            entity.HasIndex(e => e.Name, "permission_levels_name_key").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("name");

            entity.HasMany(d => d.Permissions).WithMany(p => p.PermissionLevels)
                .UsingEntity<Dictionary<string, object>>(
                    "PermissionLevelDefault",
                    r => r.HasOne<Permission>().WithMany()
                        .HasForeignKey("PermissionId")
                        .HasConstraintName("permission_level_defaults_permission_id_fkey"),
                    l => l.HasOne<PermissionLevel>().WithMany()
                        .HasForeignKey("PermissionLevelId")
                        .HasConstraintName("permission_level_defaults_permission_level_id_fkey"),
                    j =>
                    {
                        j.HasKey("PermissionLevelId", "PermissionId").HasName("permission_level_defaults_pkey");
                        j.ToTable("permission_level_defaults", tb => tb.HasComment("Permission m?c d?nh theo level. Backend seed admin_permissions t? d�y khi t?o admin m?i."));
                        j.IndexerProperty<short>("PermissionLevelId").HasColumnName("permission_level_id");
                        j.IndexerProperty<int>("PermissionId").HasColumnName("permission_id");
                    });
        });

        modelBuilder.Entity<PostComment>(entity =>
        {
            entity.HasKey(e => e.InternalId).HasName("post_comments_pkey");

            entity.ToTable("post_comments", tb => tb.HasComment("B�nh lu?n b�i dang. H? tr? 1 c?p reply qua parent_comment_internal_id."));

            entity.HasIndex(e => e.ParentCommentInternalId, "idx_comments_parent");

            entity.HasIndex(e => new { e.PostInternalId, e.CreatedAt }, "idx_comments_post");

            entity.HasIndex(e => e.Id, "post_comments_id_key").IsUnique();

            entity.Property(e => e.InternalId).HasColumnName("internal_id");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.IsHidden)
                .HasDefaultValue(false)
                .HasColumnName("is_hidden");
            entity.Property(e => e.ParentCommentInternalId).HasColumnName("parent_comment_internal_id");
            entity.Property(e => e.PostInternalId).HasColumnName("post_internal_id");
            entity.Property(e => e.UserInternalId).HasColumnName("user_internal_id");

            entity.HasOne(d => d.ParentCommentInternal).WithMany(p => p.InverseParentCommentInternal)
                .HasForeignKey(d => d.ParentCommentInternalId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("post_comments_parent_comment_internal_id_fkey");

            entity.HasOne(d => d.PostInternal).WithMany(p => p.PostComments)
                .HasForeignKey(d => d.PostInternalId)
                .HasConstraintName("post_comments_post_internal_id_fkey");

            entity.HasOne(d => d.UserInternal).WithMany(p => p.PostComments)
                .HasForeignKey(d => d.UserInternalId)
                .HasConstraintName("post_comments_user_internal_id_fkey");
        });

        modelBuilder.Entity<PostLike>(entity =>
        {
            entity.HasKey(e => new { e.PostInternalId, e.UserInternalId }).HasName("post_likes_pkey");

            entity.ToTable("post_likes", tb => tb.HasComment("Like b�i dang. Composite PK INT d?m b?o 1 user ch? like 1 b�i 1 l?n."));

            entity.Property(e => e.PostInternalId).HasColumnName("post_internal_id");
            entity.Property(e => e.UserInternalId).HasColumnName("user_internal_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");

            entity.HasOne(d => d.PostInternal).WithMany(p => p.PostLikes)
                .HasForeignKey(d => d.PostInternalId)
                .HasConstraintName("post_likes_post_internal_id_fkey");

            entity.HasOne(d => d.UserInternal).WithMany(p => p.PostLikes)
                .HasForeignKey(d => d.UserInternalId)
                .HasConstraintName("post_likes_user_internal_id_fkey");
        });

        modelBuilder.Entity<PostReport>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("post_reports_pkey");

            entity.ToTable("post_reports", tb => tb.HasComment("Report vi ph?m. Moderator xem queue v� x? l� t?ng report."));

            entity.HasIndex(e => new { e.IsResolved, e.CreatedAt }, "idx_reports_unresolved").HasFilter("(is_resolved = false)");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.IsResolved)
                .HasDefaultValue(false)
                .HasColumnName("is_resolved");
            entity.Property(e => e.PostInternalId).HasColumnName("post_internal_id");
            entity.Property(e => e.Reason)
                .HasMaxLength(100)
                .HasColumnName("reason");
            entity.Property(e => e.ReporterInternalId).HasColumnName("reporter_internal_id");
            entity.Property(e => e.ResolvedAt).HasColumnName("resolved_at");
            entity.Property(e => e.ResolvedByInternal).HasColumnName("resolved_by_internal");

            entity.HasOne(d => d.PostInternal).WithMany(p => p.PostReports)
                .HasForeignKey(d => d.PostInternalId)
                .HasConstraintName("post_reports_post_internal_id_fkey");

            entity.HasOne(d => d.ReporterInternal).WithMany(p => p.PostReportReporterInternals)
                .HasForeignKey(d => d.ReporterInternalId)
                .HasConstraintName("post_reports_reporter_internal_id_fkey");

            entity.HasOne(d => d.ResolvedByInternalNavigation).WithMany(p => p.PostReportResolvedByInternalNavigations)
                .HasForeignKey(d => d.ResolvedByInternal)
                .HasConstraintName("post_reports_resolved_by_internal_fkey");
        });

        modelBuilder.Entity<PremiumSubscription>(entity =>
        {
            entity.HasKey(e => e.InternalId).HasName("premium_subscriptions_pkey");

            entity.ToTable("premium_subscriptions", tb => tb.HasComment("G�i Premium. Check is_active + expires_at d? enforce gi?i h?n freemium."));

            entity.HasIndex(e => e.ExpiresAt, "idx_subscriptions_expiry").HasFilter("(is_active = true)");

            entity.HasIndex(e => new { e.UserInternalId, e.IsActive }, "idx_subscriptions_user");

            entity.HasIndex(e => e.Id, "premium_subscriptions_id_key").IsUnique();

            entity.Property(e => e.InternalId).HasColumnName("internal_id");
            entity.Property(e => e.CancelledAt).HasColumnName("cancelled_at");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Currency)
                .HasMaxLength(3)
                .HasDefaultValueSql("'VND'::character varying")
                .HasColumnName("currency");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.PaymentMethod)
                .HasMaxLength(50)
                .HasColumnName("payment_method");
            entity.Property(e => e.PaymentRef)
                .HasMaxLength(255)
                .HasColumnName("payment_ref");
            entity.Property(e => e.PricePaid)
                .HasPrecision(10, 2)
                .HasColumnName("price_paid");
            entity.Property(e => e.StartedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("started_at");
            entity.Property(e => e.UserInternalId).HasColumnName("user_internal_id");
            entity.Property(e => e.SubscriptionPlanInternalId).HasColumnName("subscription_plan_internal_id");

            entity.HasOne(d => d.UserInternal).WithMany(p => p.PremiumSubscriptions)
                .HasForeignKey(d => d.UserInternalId)
                .HasConstraintName("premium_subscriptions_user_internal_id_fkey");

            entity.HasOne(d => d.SubscriptionPlan).WithMany(p => p.PremiumSubscriptions)
                .HasForeignKey(d => d.SubscriptionPlanInternalId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("premium_subscriptions_subscription_plan_internal_id_fkey");
        });

        modelBuilder.Entity<SubscriptionPlan>(entity =>
        {
            entity.HasKey(e => e.InternalId).HasName("subscription_plans_pkey");

            entity.ToTable("subscription_plans", tb => tb.HasComment("Bảng cấu hình gói Premium phục vụ thanh toán."));

            entity.HasIndex(e => e.Id, "subscription_plans_id_key").IsUnique();

            entity.Property(e => e.InternalId).HasColumnName("internal_id");
            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Name).HasMaxLength(100).HasColumnName("name");
            entity.Property(e => e.Description).HasMaxLength(500).HasColumnName("description");
            entity.Property(e => e.Price).HasPrecision(10, 2).HasColumnName("price");
            entity.Property(e => e.Currency).HasMaxLength(10).HasDefaultValue("VND").HasColumnName("currency");
            entity.Property(e => e.DurationDays).HasColumnName("duration_days");
            entity.Property(e => e.IsActive).HasDefaultValue(true).HasColumnName("is_active");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()").HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()").HasColumnName("updated_at");

            // Seed default plans
            entity.HasData(
                new SubscriptionPlan
                {
                    InternalId = 1,
                    Id = Guid.Parse("3f5f3e9c-502a-43c2-bf72-351faab24c8b"),
                    Name = "Gói Tháng Premium",
                    Description = "Mở khóa toàn bộ tính năng và giới hạn tủ đồ trong 30 ngày.",
                    Price = 49000m,
                    Currency = "VND",
                    DurationDays = 30,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new SubscriptionPlan
                {
                    InternalId = 2,
                    Id = Guid.Parse("b0d61ca5-408a-4084-9dbb-8cd9c13b19ff"),
                    Name = "Gói Năm Premium",
                    Description = "Mở khóa toàn bộ tính năng và giới hạn tủ đồ trong 365 ngày (Tiết kiệm hơn).",
                    Price = 399000m,
                    Currency = "VND",
                    DurationDays = 365,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            );
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("refresh_tokens_pkey");

            entity.ToTable("refresh_tokens", tb => tb.HasComment("JWT refresh token theo thi?t b?. Logout t? xa, revoke token b?t thu?ng."));

            entity.HasIndex(e => e.TokenHash, "idx_refresh_tokens_token");

            entity.HasIndex(e => e.UserInternalId, "idx_refresh_tokens_user");

            entity.HasIndex(e => e.TokenHash, "refresh_tokens_token_hash_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DeviceInfo).HasColumnName("device_info");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.IpAddress).HasColumnName("ip_address");
            entity.Property(e => e.RevokedAt).HasColumnName("revoked_at");
            entity.Property(e => e.TokenHash)
                .HasMaxLength(255)
                .HasColumnName("token_hash");
            entity.Property(e => e.UserInternalId).HasColumnName("user_internal_id");

            entity.HasOne(d => d.UserInternal).WithMany(p => p.RefreshTokens)
                .HasForeignKey(d => d.UserInternalId)
                .HasConstraintName("refresh_tokens_user_internal_id_fkey");
        });

        modelBuilder.Entity<SponsoredCampaign>(entity =>
        {
            entity.HasKey(e => e.InternalId).HasName("sponsored_campaigns_pkey");

            entity.ToTable("sponsored_campaigns", tb => tb.HasComment("Campaign qu?ng c�o brand partner. display_rank quy?t d?nh th? t? Tab Kh�m Ph�."));

            entity.HasIndex(e => new { e.IsActive, e.StartAt, e.EndAt }, "idx_sponsored_active");

            entity.HasIndex(e => e.BrandInternalId, "idx_sponsored_brand");

            entity.HasIndex(e => e.Id, "sponsored_campaigns_id_key").IsUnique();

            entity.Property(e => e.InternalId).HasColumnName("internal_id");
            entity.Property(e => e.AffiliateProductInternalId).HasColumnName("affiliate_product_internal_id");
            entity.Property(e => e.BrandInternalId).HasColumnName("brand_internal_id");
            entity.Property(e => e.ClickCount)
                .HasDefaultValue(0)
                .HasColumnName("click_count");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DailyBudget)
                .HasPrecision(12, 2)
                .HasColumnName("daily_budget");
            entity.Property(e => e.DisplayRank)
                .HasDefaultValue((short)99)
                .HasColumnName("display_rank");
            entity.Property(e => e.EndAt).HasColumnName("end_at");
            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.ImpressionCount)
                .HasDefaultValue(0)
                .HasColumnName("impression_count");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.StartAt).HasColumnName("start_at");
            entity.Property(e => e.TotalSpent)
                .HasPrecision(12, 2)
                .HasColumnName("total_spent");

            entity.HasOne(d => d.AffiliateProductInternal).WithMany(p => p.SponsoredCampaigns)
                .HasForeignKey(d => d.AffiliateProductInternalId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("sponsored_campaigns_affiliate_product_internal_id_fkey");

            entity.HasOne(d => d.BrandInternal).WithMany(p => p.SponsoredCampaigns)
                .HasForeignKey(d => d.BrandInternalId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("sponsored_campaigns_brand_internal_id_fkey");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.InternalId).HasName("users_pkey");

            entity.ToTable("users", tb => tb.HasComment("B?ng g?c t�i kho?n. internal_id l� PK th?t d�ng cho FK. id UUID ch? d�ng cho API/URL."));

            entity.HasIndex(e => e.Email, "idx_users_email");

            entity.HasIndex(e => e.GoogleId, "idx_users_google").HasFilter("(google_id IS NOT NULL)");

            entity.HasIndex(e => e.Id, "idx_users_uuid");

            entity.HasIndex(e => e.Email, "users_email_key").IsUnique();

            entity.HasIndex(e => e.GoogleId, "users_google_id_key").IsUnique();

            entity.HasIndex(e => e.Id, "users_id_key").IsUnique();

            entity.Property(e => e.InternalId).HasColumnName("internal_id");
            entity.Property(e => e.AvatarUrl).HasColumnName("avatar_url");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DisplayName)
                .HasMaxLength(100)
                .HasColumnName("display_name");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
            entity.Property(e => e.GoogleId)
                .HasMaxLength(255)
                .HasColumnName("google_id");
            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.IsEmailVerified)
                .HasDefaultValue(false)
                .HasColumnName("is_email_verified");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .HasColumnName("password_hash");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.Property(e => e.AgreedToTermsAt).HasColumnName("agreed_to_terms_at");
            entity.Property(e => e.TermsVersion)
                .HasMaxLength(50)
                .HasColumnName("terms_version");
            entity.Property(e => e.AgreedToTermsIp)
                .HasMaxLength(45)
                .HasColumnName("agreed_to_terms_ip");
        });

        modelBuilder.Entity<UserBanLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("user_ban_logs_pkey");

            entity.ToTable("user_ban_logs", tb => tb.HasComment("L?ch s? kho�/m? kho�. Audit log d? moderator gi?i tr�nh v� xem pattern vi ph?m."));

            entity.HasIndex(e => new { e.UserInternalId, e.BanType }, "idx_ban_logs_active").HasFilter("(is_lifted = false)");

            entity.HasIndex(e => new { e.UserInternalId, e.CreatedAt }, "idx_ban_logs_user").IsDescending(false, true);

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.BanType)
                .HasMaxLength(20)
                .HasColumnName("ban_type");
            entity.Property(e => e.BannedByInternal).HasColumnName("banned_by_internal");
            entity.Property(e => e.BannedUntil).HasColumnName("banned_until");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.IsLifted)
                .HasDefaultValue(false)
                .HasColumnName("is_lifted");
            entity.Property(e => e.LiftReason).HasColumnName("lift_reason");
            entity.Property(e => e.LiftedAt).HasColumnName("lifted_at");
            entity.Property(e => e.LiftedByInternal).HasColumnName("lifted_by_internal");
            entity.Property(e => e.Reason).HasColumnName("reason");
            entity.Property(e => e.UserInternalId).HasColumnName("user_internal_id");

            entity.HasOne(d => d.BannedByInternalNavigation).WithMany(p => p.UserBanLogBannedByInternalNavigations)
                .HasForeignKey(d => d.BannedByInternal)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("user_ban_logs_banned_by_internal_fkey");

            entity.HasOne(d => d.LiftedByInternalNavigation).WithMany(p => p.UserBanLogLiftedByInternalNavigations)
                .HasForeignKey(d => d.LiftedByInternal)
                .HasConstraintName("user_ban_logs_lifted_by_internal_fkey");

            entity.HasOne(d => d.UserInternal).WithMany(p => p.UserBanLogUserInternals)
                .HasForeignKey(d => d.UserInternalId)
                .HasConstraintName("user_ban_logs_user_internal_id_fkey");
        });

        modelBuilder.Entity<WardrobeItem>(entity =>
        {
            entity.HasKey(e => e.InternalId).HasName("wardrobe_items_pkey");

            entity.ToTable("wardrobe_items", tb => tb.HasComment("T? d? s?. M?i item c� ?nh g?c v� ?nh d� x�a n?n d? gh�p canvas/mannequin."));

            entity.HasIndex(e => new { e.UserInternalId, e.IsActive }, "idx_wardrobe_active");

            entity.HasIndex(e => e.UserInternalId, "idx_wardrobe_user");

            entity.HasIndex(e => e.Id, "idx_wardrobe_uuid");

            entity.HasIndex(e => e.Id, "wardrobe_items_id_key").IsUnique();

            entity.Property(e => e.InternalId).HasColumnName("internal_id");
            entity.Property(e => e.Brand)
                .HasMaxLength(100)
                .HasColumnName("brand");
            entity.Property(e => e.ColorTags).HasColumnName("color_tags");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.OriginalImageUrl).HasColumnName("original_image_url");
            entity.Property(e => e.RemovedBgUrl).HasColumnName("removed_bg_url");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserInternalId).HasColumnName("user_internal_id");

            entity.HasOne(d => d.UserInternal).WithMany(p => p.WardrobeItems)
                .HasForeignKey(d => d.UserInternalId)
                .HasConstraintName("wardrobe_items_user_internal_id_fkey");
        });

        modelBuilder.Entity<PaymentTransaction>(entity =>
        {
            entity.HasKey(e => e.InternalId).HasName("payment_transactions_pkey");

            entity.ToTable("payment_transactions", tb => tb.HasComment("Bảng ghi nhận giao dịch thanh toán qua ví điện tử"));

            entity.HasIndex(e => e.Id, "payment_transactions_id_key").IsUnique();

            entity.Property(e => e.InternalId).HasColumnName("internal_id");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");

            entity.Property(e => e.UserInternalId).HasColumnName("user_internal_id");

            entity.Property(e => e.SubscriptionPlanInternalId).HasColumnName("subscription_plan_internal_id");

            entity.Property(e => e.Amount)
                .HasPrecision(10, 2)
                .HasColumnName("amount");

            entity.Property(e => e.Currency)
                .HasMaxLength(3)
                .HasDefaultValueSql("'VND'::character varying")
                .HasColumnName("currency");

            entity.Property(e => e.PaymentGateway)
                .HasMaxLength(50)
                .HasColumnName("payment_gateway");

            entity.Property(e => e.Status)
                .HasColumnName("status");

            entity.Property(e => e.GatewayTransactionId)
                .HasMaxLength(255)
                .HasColumnName("gateway_transaction_id");

            entity.Property(e => e.RawCallbackData)
                .HasColumnType("text")
                .HasColumnName("raw_callback_data");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");

            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.UserInternal).WithMany(p => p.PaymentTransactions)
                .HasForeignKey(d => d.UserInternalId)
                .HasConstraintName("payment_transactions_user_internal_id_fkey");

            entity.HasOne(d => d.SubscriptionPlan).WithMany(p => p.PaymentTransactions)
                .HasForeignKey(d => d.SubscriptionPlanInternalId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("payment_transactions_subscription_plan_internal_id_fkey");
        });

        modelBuilder.Entity<SubscriptionTierConfig>(entity =>
        {
            entity.ToTable("subscription_tier_configs");

            entity.HasKey(e => e.InternalId);

            entity.Property(e => e.InternalId)
                .UseIdentityByDefaultColumn()
                .HasColumnName("internal_id");

            entity.HasIndex(e => e.TierName).IsUnique();

            entity.Property(e => e.TierName)
                .HasMaxLength(50)
                .HasColumnName("tier_name");

            entity.Property(e => e.BgRemovalCredits)
                .HasDefaultValue(1)
                .HasColumnName("bg_removal_credits");

            entity.Property(e => e.TryOnCredits)
                .HasDefaultValue(1)
                .HasColumnName("try_on_credits");

            entity.Property(e => e.WardrobeItemLimit)
                .HasColumnName("wardrobe_item_limit");

            entity.Property(e => e.OutfitLimit)
                .HasColumnName("outfit_limit");

            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(256)
                .HasColumnName("updated_by");

            // Seed data mặc định
            entity.HasData(
                new SubscriptionTierConfig
                {
                    InternalId         = 1,
                    TierName           = "free",
                    BgRemovalCredits   = 1,
                    TryOnCredits       = 1,
                    WardrobeItemLimit  = 2,
                    OutfitLimit        = 2,
                    UpdatedAt          = new DateTime(2026, 6, 4, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedBy          = "system"
                },
                new SubscriptionTierConfig
                {
                    InternalId         = 2,
                    TierName           = "premium",
                    BgRemovalCredits   = 2,
                    TryOnCredits       = 2,
                    WardrobeItemLimit  = null,
                    OutfitLimit        = null,
                    UpdatedAt          = new DateTime(2026, 6, 4, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedBy          = "system"
                }
            );
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}

