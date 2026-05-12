-- =============================================================================
-- V-CLOSET DATABASE SCHEMA — PostgreSQL v3.0
-- Nhóm Sentinels
-- =============================================================================
-- CHIẾN LƯỢC KHÓA v2.0:
--   • id UUID             → expose ra ngoài (URL, API response) — không đoán được
--   • internal_id SERIAL  → PRIMARY KEY thật sự, dùng cho toàn bộ FK và JOIN nội bộ
--
-- Lý do đổi sang INT cho FK:
--   • UUID (16 bytes) vs INT (4 bytes) — nhẹ hơn 4x cho mỗi FK column
--   • Index INT B-tree nhanh hơn UUID B-tree đáng kể ở hàng triệu bản ghi
--   • JOIN nhiều bảng lồng nhau (canvas → outfit_items → wardrobe → user)
--     sẽ rõ ràng lợi hơn khi dùng INT
--   • UUID vẫn có index riêng để lookup từ API
-- =============================================================================

CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- =============================================================================
-- PHẦN 0: ENUM TYPES
-- =============================================================================

CREATE TYPE user_role         AS ENUM ('customer', 'admin', 'moderator', 'brand_partner');
CREATE TYPE auth_provider     AS ENUM ('local', 'google');
CREATE TYPE body_shape_type   AS ENUM ('hourglass', 'pear', 'apple', 'rectangle', 'inverted_triangle');
CREATE TYPE clothing_category AS ENUM ('top', 'bottom', 'dress', 'outerwear', 'shoes', 'bag', 'accessory', 'other');
CREATE TYPE ai_job_status     AS ENUM ('pending', 'processing', 'completed', 'failed');
CREATE TYPE chat_room_type    AS ENUM ('public', 'topic', 'direct');
CREATE TYPE message_type      AS ENUM ('text', 'image', 'outfit_share', 'system');
CREATE TYPE commission_status AS ENUM ('pending', 'confirmed', 'paid', 'rejected');
CREATE TYPE premium_plan      AS ENUM ('monthly', 'yearly');
CREATE TYPE brand_status      AS ENUM ('pending', 'verified', 'suspended');


-- =============================================================================
-- PHẦN 1: IDENTITY & AUTH
-- =============================================================================

-- -----------------------------------------------------------------------------
-- Bảng: users
-- internal_id SERIAL = PK thật, dùng cho mọi FK trong hệ thống
-- id UUID = chỉ expose ra API/URL, không dùng để JOIN
-- -----------------------------------------------------------------------------
CREATE TABLE users (
    internal_id       SERIAL        PRIMARY KEY,
    id                UUID          NOT NULL UNIQUE DEFAULT gen_random_uuid(),
    email             VARCHAR(255)  NOT NULL UNIQUE,
    password_hash     VARCHAR(255),
    google_id         VARCHAR(255)  UNIQUE,
    auth_provider     auth_provider NOT NULL DEFAULT 'local',
    display_name      VARCHAR(100)  NOT NULL,
    avatar_url        TEXT,
    role              user_role     NOT NULL DEFAULT 'customer',
    is_active         BOOLEAN       NOT NULL DEFAULT TRUE,
    is_email_verified BOOLEAN       NOT NULL DEFAULT FALSE,
    created_at        TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    updated_at        TIMESTAMPTZ   NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_users_uuid   ON users(id);
CREATE INDEX idx_users_email  ON users(email);
CREATE INDEX idx_users_role   ON users(role);
CREATE INDEX idx_users_google ON users(google_id) WHERE google_id IS NOT NULL;

COMMENT ON TABLE users IS 'Bảng gốc tài khoản. internal_id là PK thật dùng cho FK. id UUID chỉ dùng cho API/URL.';


-- -----------------------------------------------------------------------------
-- Bảng: permission_levels
-- Seed trước admin_profiles vì admin_profiles FK đến đây
-- -----------------------------------------------------------------------------
CREATE TABLE permission_levels (
    id          SMALLINT     PRIMARY KEY,
    name        VARCHAR(50)  NOT NULL UNIQUE,
    description TEXT,
    created_at  TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);

INSERT INTO permission_levels (id, name, description) VALUES
(1, 'moderator',   'Duyệt/xóa nội dung cộng đồng, xử lý report vi phạm'),
(2, 'admin',       'Quản lý user, duyệt brand partner, xem analytics toàn hệ thống'),
(3, 'super_admin', 'Toàn quyền hệ thống, phân quyền admin khác');

COMMENT ON TABLE permission_levels IS 'Cấp quyền tổng thể cho admin/moderator. FK từ admin_profiles.';


-- -----------------------------------------------------------------------------
-- Bảng: permissions
-- Danh mục permission chi tiết — admin controller CRUD bảng này
-- code dạng 'group.action' để dùng trong C# [RequirePermission("brand.create")]
-- -----------------------------------------------------------------------------
CREATE TABLE permissions (
    id          SERIAL        PRIMARY KEY,
    code        VARCHAR(100)  NOT NULL UNIQUE,
    name        VARCHAR(255)  NOT NULL,
    description TEXT,
    grp         VARCHAR(50)   NOT NULL,
    created_at  TIMESTAMPTZ   NOT NULL DEFAULT NOW()
);

INSERT INTO permissions (code, name, description, grp) VALUES
('user.view',        'Xem danh sách user',           'Xem toàn bộ danh sách và chi tiết user',             'user'),
('user.ban',         'Khoá chat/post của user',       'Khoá tạm thời hoặc vĩnh viễn chat, đăng bài',       'user'),
('user.deactivate',  'Vô hiệu hoá tài khoản',        'Tắt toàn bộ tài khoản user (is_active = false)',     'user'),
('brand.create',     'Tạo tài khoản brand partner',  'Tạo user mới với role brand_partner',                'brand'),
('brand.verify',     'Duyệt brand partner',           'Duyệt hoặc từ chối đăng ký brand partner',          'brand'),
('brand.suspend',    'Đình chỉ brand partner',        'Tạm dừng hoạt động của brand partner',              'brand'),
('content.moderate', 'Kiểm duyệt nội dung',          'Ẩn/xóa bài viết, bình luận vi phạm',                'content'),
('content.report',   'Xử lý report vi phạm',         'Xem queue report và đánh dấu đã xử lý',             'content'),
('analytics.view',   'Xem báo cáo doanh thu',        'Truy cập dashboard affiliate, commission, revenue',  'analytics'),
('analytics.export', 'Xuất báo cáo',                 'Export CSV/Excel báo cáo doanh thu',                 'analytics'),
('admin.create',     'Tạo tài khoản admin/moderator','Tạo user mới với role admin hoặc moderator',         'admin'),
('permission.grant', 'Cấp/thu hồi quyền',            'Gán hoặc thu hồi permission của admin khác',        'admin');

COMMENT ON TABLE permissions IS 'Danh mục permission. code dạng group.action dùng trong C# RequirePermission attribute.';


-- -----------------------------------------------------------------------------
-- Bảng: permission_level_defaults
-- Map level → permission mặc định khi tạo admin mới
-- Backend tự seed admin_permissions từ bảng này — không cần gán tay từng quyền
-- -----------------------------------------------------------------------------
CREATE TABLE permission_level_defaults (
    permission_level_id SMALLINT NOT NULL REFERENCES permission_levels(id) ON DELETE CASCADE,
    permission_id       INT      NOT NULL REFERENCES permissions(id)        ON DELETE CASCADE,
    PRIMARY KEY (permission_level_id, permission_id)
);

-- moderator mặc định
INSERT INTO permission_level_defaults
SELECT 1, id FROM permissions
WHERE code IN ('user.view', 'user.ban', 'content.moderate', 'content.report');

-- admin mặc định
INSERT INTO permission_level_defaults
SELECT 2, id FROM permissions
WHERE code IN (
    'user.view', 'user.ban', 'user.deactivate',
    'brand.create', 'brand.verify', 'brand.suspend',
    'content.moderate', 'content.report',
    'analytics.view', 'analytics.export'
);

-- super_admin có tất cả
INSERT INTO permission_level_defaults
SELECT 3, id FROM permissions;

COMMENT ON TABLE permission_level_defaults IS 'Permission mặc định theo level. Backend seed admin_permissions từ đây khi tạo admin mới.';


-- -----------------------------------------------------------------------------
-- Bảng: customer_profiles — FK dùng user internal_id (INT)
-- -----------------------------------------------------------------------------
CREATE TABLE customer_profiles (
    internal_id             SERIAL       PRIMARY KEY,
    id                      UUID         NOT NULL UNIQUE DEFAULT gen_random_uuid(),
    user_internal_id        INT          NOT NULL UNIQUE REFERENCES users(internal_id) ON DELETE CASCADE,
    height_cm               DECIMAL(5,2),
    weight_kg               DECIMAL(5,2),
    body_shape              body_shape_type,
    mannequin_image_url     TEXT,
    mannequin_generated_at  TIMESTAMPTZ,
    wardrobe_item_count     INT          NOT NULL DEFAULT 0,
    is_chat_banned          BOOLEAN      NOT NULL DEFAULT FALSE,
    is_post_banned          BOOLEAN      NOT NULL DEFAULT FALSE,
    chat_banned_until       TIMESTAMPTZ,
    post_banned_until       TIMESTAMPTZ,
    updated_at              TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);

COMMENT ON TABLE customer_profiles IS 'Profile customer: số đo, mannequin AI, trạng thái ban. FK dùng INT.';
COMMENT ON COLUMN customer_profiles.wardrobe_item_count IS 'Cache để check giới hạn freemium 50 items mà không COUNT(*).';
COMMENT ON COLUMN customer_profiles.is_chat_banned      IS 'TRUE = bị khoá chat. Kết hợp chat_banned_until phân biệt tạm thời/vĩnh viễn.';
COMMENT ON COLUMN customer_profiles.is_post_banned      IS 'TRUE = bị khoá đăng bài. Kết hợp post_banned_until phân biệt tạm thời/vĩnh viễn.';


-- -----------------------------------------------------------------------------
-- Bảng: admin_profiles — FK dùng user internal_id (INT)
-- -----------------------------------------------------------------------------
CREATE TABLE admin_profiles (
    internal_id      SERIAL      PRIMARY KEY,
    id               UUID        NOT NULL UNIQUE DEFAULT gen_random_uuid(),
    user_internal_id INT         NOT NULL UNIQUE REFERENCES users(internal_id) ON DELETE CASCADE,
    permission_level SMALLINT    NOT NULL DEFAULT 1 REFERENCES permission_levels(id),
    department       VARCHAR(100),
    notes            TEXT,
    created_at       TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

COMMENT ON TABLE admin_profiles IS 'Profile admin/moderator. permission_level là vai trò tổng thể, chi tiết ở admin_permissions.';


-- -----------------------------------------------------------------------------
-- Bảng: admin_permissions — FK dùng user internal_id (INT)
-- granted_by_internal = audit trail ai đã cấp quyền
-- -----------------------------------------------------------------------------
CREATE TABLE admin_permissions (
    user_internal_id    INT         NOT NULL REFERENCES users(internal_id) ON DELETE CASCADE,
    permission_id       INT         NOT NULL REFERENCES permissions(id)    ON DELETE CASCADE,
    granted_by_internal INT         NOT NULL REFERENCES users(internal_id),
    granted_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    PRIMARY KEY (user_internal_id, permission_id)
);

CREATE INDEX idx_admin_permissions_user ON admin_permissions(user_internal_id);

COMMENT ON TABLE admin_permissions IS 'Permission cụ thể từng admin. Composite PK INT. granted_by_internal là audit trail.';


-- -----------------------------------------------------------------------------
-- Bảng: brand_profiles — FK dùng user internal_id (INT)
-- -----------------------------------------------------------------------------
CREATE TABLE brand_profiles (
    internal_id      SERIAL        PRIMARY KEY,
    id               UUID          NOT NULL UNIQUE DEFAULT gen_random_uuid(),
    user_internal_id INT           NOT NULL UNIQUE REFERENCES users(internal_id) ON DELETE CASCADE,
    brand_name       VARCHAR(255)  NOT NULL,
    logo_url         TEXT,
    website_url      TEXT,
    contact_phone    VARCHAR(20),
    tax_code         VARCHAR(50),
    status           brand_status  NOT NULL DEFAULT 'pending',
    credit_balance   DECIMAL(12,2) NOT NULL DEFAULT 0,
    created_at       TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    updated_at       TIMESTAMPTZ   NOT NULL DEFAULT NOW()
);

COMMENT ON TABLE brand_profiles IS 'Profile brand partner B2B. Admin verify trước khi chạy sponsored campaign.';


-- -----------------------------------------------------------------------------
-- Bảng: refresh_tokens — FK dùng user internal_id (INT)
-- -----------------------------------------------------------------------------
CREATE TABLE refresh_tokens (
    id               UUID         PRIMARY KEY DEFAULT gen_random_uuid(),
    user_internal_id INT          NOT NULL REFERENCES users(internal_id) ON DELETE CASCADE,
    token_hash       VARCHAR(255) NOT NULL UNIQUE,
    device_info      TEXT,
    ip_address       INET,
    expires_at       TIMESTAMPTZ  NOT NULL,
    revoked_at       TIMESTAMPTZ,
    created_at       TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_refresh_tokens_user  ON refresh_tokens(user_internal_id);
CREATE INDEX idx_refresh_tokens_token ON refresh_tokens(token_hash);

COMMENT ON TABLE refresh_tokens IS 'JWT refresh token theo thiết bị. Logout từ xa, revoke token bất thường.';


-- =============================================================================
-- PHẦN 2: AFFILIATE PRODUCTS
-- Tạo trước wardrobe/canvas vì canvas_outfit_items FK đến đây
-- =============================================================================

CREATE TABLE affiliate_products (
    internal_id       SERIAL        PRIMARY KEY,
    id                UUID          NOT NULL UNIQUE DEFAULT gen_random_uuid(),
    shopee_product_id VARCHAR(100)  NOT NULL UNIQUE,
    shopee_shop_id    VARCHAR(100),
    name              VARCHAR(500)  NOT NULL,
    description       TEXT,
    image_url         TEXT          NOT NULL,
    price             DECIMAL(12,2) NOT NULL,
    original_price    DECIMAL(12,2),
    category          clothing_category,
    affiliate_link    TEXT          NOT NULL,
    tracking_code     VARCHAR(100)  NOT NULL UNIQUE,
    click_count       INT           NOT NULL DEFAULT 0,
    conversion_count  INT           NOT NULL DEFAULT 0,
    is_trending       BOOLEAN       NOT NULL DEFAULT FALSE,
    is_active         BOOLEAN       NOT NULL DEFAULT TRUE,
    synced_at         TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    created_at        TIMESTAMPTZ   NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_affiliate_trending ON affiliate_products(is_trending, is_active);
CREATE INDEX idx_affiliate_category ON affiliate_products(category, is_active);
CREATE INDEX idx_affiliate_tracking ON affiliate_products(tracking_code);
CREATE INDEX idx_affiliate_uuid     ON affiliate_products(id);

COMMENT ON TABLE affiliate_products IS 'Sản phẩm trending sync từ Shopee mỗi đêm. Tạo trước canvas_outfit_items vì có FK phụ thuộc.';


-- =============================================================================
-- PHẦN 3: WARDROBE & AI CORE
-- =============================================================================

CREATE TABLE wardrobe_items (
    internal_id        SERIAL          PRIMARY KEY,
    id                 UUID            NOT NULL UNIQUE DEFAULT gen_random_uuid(),
    user_internal_id   INT             NOT NULL REFERENCES users(internal_id) ON DELETE CASCADE,
    name               VARCHAR(255),
    original_image_url TEXT            NOT NULL,
    removed_bg_url     TEXT,
    bg_removal_status  ai_job_status   NOT NULL DEFAULT 'pending',
    category           clothing_category NOT NULL DEFAULT 'other',
    color_tags         TEXT[],
    brand              VARCHAR(100),
    notes              TEXT,
    is_active          BOOLEAN         NOT NULL DEFAULT TRUE,
    created_at         TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    updated_at         TIMESTAMPTZ     NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_wardrobe_user     ON wardrobe_items(user_internal_id);
CREATE INDEX idx_wardrobe_category ON wardrobe_items(user_internal_id, category);
CREATE INDEX idx_wardrobe_active   ON wardrobe_items(user_internal_id, is_active);
CREATE INDEX idx_wardrobe_uuid     ON wardrobe_items(id);

COMMENT ON TABLE wardrobe_items IS 'Tủ đồ số. Mỗi item có ảnh gốc và ảnh đã xóa nền để ghép canvas/mannequin.';


CREATE TABLE canvas_outfits (
    internal_id         SERIAL      PRIMARY KEY,
    id                  UUID        NOT NULL UNIQUE DEFAULT gen_random_uuid(),
    user_internal_id    INT         NOT NULL REFERENCES users(internal_id) ON DELETE CASCADE,
    title               VARCHAR(255),
    canvas_snapshot_url TEXT,
    is_public           BOOLEAN     NOT NULL DEFAULT FALSE,
    like_count          INT         NOT NULL DEFAULT 0,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at          TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_canvas_outfits_user   ON canvas_outfits(user_internal_id);
CREATE INDEX idx_canvas_outfits_public ON canvas_outfits(is_public, created_at DESC);
CREATE INDEX idx_canvas_outfits_uuid   ON canvas_outfits(id);

COMMENT ON TABLE canvas_outfits IS 'Outfit tạo từ Canvas 2D. Chứa đồ từ tủ nhà và đồ trending affiliate.';


-- affiliate_products đã tạo ở Phần 2 nên cả 2 FK đều hợp lệ tại đây
CREATE TABLE canvas_outfit_items (
    id                            SERIAL       PRIMARY KEY,
    outfit_internal_id            INT          NOT NULL REFERENCES canvas_outfits(internal_id)    ON DELETE CASCADE,
    wardrobe_item_internal_id     INT          REFERENCES wardrobe_items(internal_id)              ON DELETE SET NULL,
    affiliate_product_internal_id INT          REFERENCES affiliate_products(internal_id)          ON DELETE SET NULL,
    pos_x                         DECIMAL(8,2) NOT NULL DEFAULT 0,
    pos_y                         DECIMAL(8,2) NOT NULL DEFAULT 0,
    scale                         DECIMAL(4,2) NOT NULL DEFAULT 1.0,
    rotation                      DECIMAL(6,2) NOT NULL DEFAULT 0,
    z_index                       SMALLINT     NOT NULL DEFAULT 0,
    CONSTRAINT chk_item_source CHECK (
        (wardrobe_item_internal_id IS NOT NULL AND affiliate_product_internal_id IS NULL) OR
        (wardrobe_item_internal_id IS NULL     AND affiliate_product_internal_id IS NOT NULL)
    )
);

CREATE INDEX idx_canvas_items_outfit ON canvas_outfit_items(outfit_internal_id);

COMMENT ON TABLE canvas_outfit_items IS 'Vị trí từng item trên canvas. Đồ nhà HOẶC affiliate — CHECK constraint đảm bảo chỉ 1 trong 2.';


CREATE TABLE ai_lookbooks (
    internal_id         SERIAL        PRIMARY KEY,
    id                  UUID          NOT NULL UNIQUE DEFAULT gen_random_uuid(),
    outfit_internal_id  INT           NOT NULL REFERENCES canvas_outfits(internal_id) ON DELETE CASCADE,
    user_internal_id    INT           NOT NULL REFERENCES users(internal_id)           ON DELETE CASCADE,
    generated_image_url TEXT,
    status              ai_job_status NOT NULL DEFAULT 'pending',
    ai_prompt_used      TEXT,
    error_message       TEXT,
    generation_seconds  DECIMAL(6,2),
    created_at          TIMESTAMPTZ   NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_lookbooks_user   ON ai_lookbooks(user_internal_id);
CREATE INDEX idx_lookbooks_outfit ON ai_lookbooks(outfit_internal_id);
CREATE INDEX idx_lookbooks_status ON ai_lookbooks(status) WHERE status = 'pending';
CREATE INDEX idx_lookbooks_uuid   ON ai_lookbooks(id);

COMMENT ON TABLE ai_lookbooks IS 'Ảnh lookbook AI generate từ canvas outfit. Lưu prompt để A/B test cải thiện model.';


-- =============================================================================
-- PHẦN 4: AFFILIATE CLICKS & CONVERSIONS
-- =============================================================================

CREATE TABLE affiliate_clicks (
    id                            UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    user_internal_id              INT         REFERENCES users(internal_id)              ON DELETE SET NULL,
    affiliate_product_internal_id INT         NOT NULL REFERENCES affiliate_products(internal_id),
    outfit_internal_id            INT         REFERENCES canvas_outfits(internal_id)     ON DELETE SET NULL,
    click_source                  VARCHAR(50) NOT NULL DEFAULT 'discovery',
    ip_address                    INET,
    user_agent                    TEXT,
    clicked_at                    TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_clicks_product ON affiliate_clicks(affiliate_product_internal_id, clicked_at DESC);
CREATE INDEX idx_clicks_user    ON affiliate_clicks(user_internal_id, clicked_at DESC);

COMMENT ON TABLE affiliate_clicks IS 'Log click affiliate. Tính CTR, match conversion, phát hiện click fraud.';


CREATE TABLE affiliate_conversions (
    id                            UUID          PRIMARY KEY DEFAULT gen_random_uuid(),
    click_id                      UUID          REFERENCES affiliate_clicks(id)           ON DELETE SET NULL,
    user_internal_id              INT           REFERENCES users(internal_id)              ON DELETE SET NULL,
    affiliate_product_internal_id INT           NOT NULL REFERENCES affiliate_products(internal_id),
    shopee_order_id               VARCHAR(100),
    order_amount                  DECIMAL(12,2) NOT NULL,
    commission_rate               DECIMAL(4,3)  NOT NULL,
    commission_amount             DECIMAL(12,2) NOT NULL,
    status                        commission_status NOT NULL DEFAULT 'pending',
    converted_at                  TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    confirmed_at                  TIMESTAMPTZ,
    paid_at                       TIMESTAMPTZ
);

CREATE INDEX idx_conversions_user    ON affiliate_conversions(user_internal_id);
CREATE INDEX idx_conversions_product ON affiliate_conversions(affiliate_product_internal_id);
CREATE INDEX idx_conversions_status  ON affiliate_conversions(status);

COMMENT ON TABLE affiliate_conversions IS 'Đơn hàng thành công qua affiliate. commission_rate snapshot tại thời điểm chuyển đổi.';


-- =============================================================================
-- PHẦN 5: B2B SPONSORSHIP
-- =============================================================================

CREATE TABLE sponsored_campaigns (
    internal_id                   SERIAL        PRIMARY KEY,
    id                            UUID          NOT NULL UNIQUE DEFAULT gen_random_uuid(),
    brand_internal_id             INT           NOT NULL REFERENCES brand_profiles(internal_id),
    affiliate_product_internal_id INT           NOT NULL REFERENCES affiliate_products(internal_id),
    display_rank                  SMALLINT      NOT NULL DEFAULT 99,
    daily_budget                  DECIMAL(12,2) NOT NULL,
    total_spent                   DECIMAL(12,2) NOT NULL DEFAULT 0,
    impression_count              INT           NOT NULL DEFAULT 0,
    click_count                   INT           NOT NULL DEFAULT 0,
    start_at                      TIMESTAMPTZ   NOT NULL,
    end_at                        TIMESTAMPTZ   NOT NULL,
    is_active                     BOOLEAN       NOT NULL DEFAULT TRUE,
    created_at                    TIMESTAMPTZ   NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_sponsored_active ON sponsored_campaigns(is_active, start_at, end_at);
CREATE INDEX idx_sponsored_brand  ON sponsored_campaigns(brand_internal_id);

COMMENT ON TABLE sponsored_campaigns IS 'Campaign quảng cáo brand partner. display_rank quyết định thứ tự Tab Khám Phá.';


CREATE TABLE campaign_impressions (
    id                   BIGSERIAL   PRIMARY KEY,
    campaign_internal_id INT         NOT NULL REFERENCES sponsored_campaigns(internal_id) ON DELETE CASCADE,
    user_internal_id     INT         REFERENCES users(internal_id) ON DELETE SET NULL,
    impressed_at         TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_impressions_campaign ON campaign_impressions(campaign_internal_id, impressed_at DESC);

COMMENT ON TABLE campaign_impressions IS 'Log impression sponsored. Volume cao — cân nhắc partition theo tháng khi scale.';


-- =============================================================================
-- PHẦN 6: FREEMIUM & BILLING
-- =============================================================================

CREATE TABLE premium_subscriptions (
    internal_id      SERIAL        PRIMARY KEY,
    id               UUID          NOT NULL UNIQUE DEFAULT gen_random_uuid(),
    user_internal_id INT           NOT NULL REFERENCES users(internal_id) ON DELETE CASCADE,
    plan_type        premium_plan  NOT NULL,
    price_paid       DECIMAL(10,2) NOT NULL,
    currency         VARCHAR(3)    NOT NULL DEFAULT 'VND',
    started_at       TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    expires_at       TIMESTAMPTZ   NOT NULL,
    is_active        BOOLEAN       NOT NULL DEFAULT TRUE,
    payment_method   VARCHAR(50),
    payment_ref      VARCHAR(255),
    cancelled_at     TIMESTAMPTZ,
    created_at       TIMESTAMPTZ   NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_subscriptions_user   ON premium_subscriptions(user_internal_id, is_active);
CREATE INDEX idx_subscriptions_expiry ON premium_subscriptions(expires_at) WHERE is_active = TRUE;

COMMENT ON TABLE premium_subscriptions IS 'Gói Premium. Check is_active + expires_at để enforce giới hạn freemium.';


-- =============================================================================
-- PHẦN 7: COMMUNITY
-- =============================================================================

CREATE TABLE community_posts (
    internal_id        SERIAL      PRIMARY KEY,
    id                 UUID        NOT NULL UNIQUE DEFAULT gen_random_uuid(),
    user_internal_id   INT         NOT NULL REFERENCES users(internal_id)        ON DELETE CASCADE,
    outfit_internal_id INT         REFERENCES canvas_outfits(internal_id)        ON DELETE SET NULL,
    caption            TEXT,
    like_count         INT         NOT NULL DEFAULT 0,
    comment_count      INT         NOT NULL DEFAULT 0,
    is_public          BOOLEAN     NOT NULL DEFAULT TRUE,
    is_hidden          BOOLEAN     NOT NULL DEFAULT FALSE,
    created_at         TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at         TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_posts_user   ON community_posts(user_internal_id, created_at DESC);
CREATE INDEX idx_posts_feed   ON community_posts(is_public, is_hidden, created_at DESC);
CREATE INDEX idx_posts_outfit ON community_posts(outfit_internal_id);
CREATE INDEX idx_posts_uuid   ON community_posts(id);

COMMENT ON TABLE community_posts IS 'Bài đăng community feed. Gắn với canvas outfit để người khác thử outfit tương tự.';


CREATE TABLE post_comments (
    internal_id                SERIAL      PRIMARY KEY,
    id                         UUID        NOT NULL UNIQUE DEFAULT gen_random_uuid(),
    post_internal_id           INT         NOT NULL REFERENCES community_posts(internal_id) ON DELETE CASCADE,
    user_internal_id           INT         NOT NULL REFERENCES users(internal_id)            ON DELETE CASCADE,
    parent_comment_internal_id INT         REFERENCES post_comments(internal_id)             ON DELETE CASCADE,
    content                    TEXT        NOT NULL,
    is_hidden                  BOOLEAN     NOT NULL DEFAULT FALSE,
    created_at                 TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_comments_post   ON post_comments(post_internal_id, created_at ASC);
CREATE INDEX idx_comments_parent ON post_comments(parent_comment_internal_id);

COMMENT ON TABLE post_comments IS 'Bình luận bài đăng. Hỗ trợ 1 cấp reply qua parent_comment_internal_id.';


CREATE TABLE post_likes (
    post_internal_id INT         NOT NULL REFERENCES community_posts(internal_id) ON DELETE CASCADE,
    user_internal_id INT         NOT NULL REFERENCES users(internal_id)            ON DELETE CASCADE,
    created_at       TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    PRIMARY KEY (post_internal_id, user_internal_id)
);

COMMENT ON TABLE post_likes IS 'Like bài đăng. Composite PK INT đảm bảo 1 user chỉ like 1 bài 1 lần.';


CREATE TABLE post_reports (
    id                   UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    post_internal_id     INT         NOT NULL REFERENCES community_posts(internal_id) ON DELETE CASCADE,
    reporter_internal_id INT         NOT NULL REFERENCES users(internal_id)            ON DELETE CASCADE,
    reason               VARCHAR(100) NOT NULL,
    description          TEXT,
    is_resolved          BOOLEAN     NOT NULL DEFAULT FALSE,
    resolved_by_internal INT         REFERENCES users(internal_id),
    resolved_at          TIMESTAMPTZ,
    created_at           TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_reports_unresolved ON post_reports(is_resolved, created_at) WHERE is_resolved = FALSE;

COMMENT ON TABLE post_reports IS 'Report vi phạm. Moderator xem queue và xử lý từng report.';


CREATE TABLE user_ban_logs (
    id                 UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    user_internal_id   INT         NOT NULL REFERENCES users(internal_id) ON DELETE CASCADE,
    banned_by_internal INT         NOT NULL REFERENCES users(internal_id),
    ban_type           VARCHAR(20) NOT NULL CHECK (ban_type IN ('chat', 'post', 'account')),
    reason             TEXT        NOT NULL,
    banned_until       TIMESTAMPTZ,
    is_lifted          BOOLEAN     NOT NULL DEFAULT FALSE,
    lifted_by_internal INT         REFERENCES users(internal_id),
    lifted_at          TIMESTAMPTZ,
    lift_reason        TEXT,
    created_at         TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_ban_logs_user   ON user_ban_logs(user_internal_id, created_at DESC);
CREATE INDEX idx_ban_logs_active ON user_ban_logs(user_internal_id, ban_type) WHERE is_lifted = FALSE;

COMMENT ON TABLE user_ban_logs IS 'Lịch sử khoá/mở khoá. Audit log để moderator giải trình và xem pattern vi phạm.';


-- =============================================================================
-- PHẦN 8: REALTIME CHAT
-- =============================================================================

CREATE TABLE chat_rooms (
    internal_id         SERIAL         PRIMARY KEY,
    id                  UUID           NOT NULL UNIQUE DEFAULT gen_random_uuid(),
    name                VARCHAR(255),
    description         TEXT,
    room_type           chat_room_type NOT NULL DEFAULT 'public',
    cover_url           TEXT,
    is_active           BOOLEAN        NOT NULL DEFAULT TRUE,
    created_by_internal INT            REFERENCES users(internal_id) ON DELETE SET NULL,
    created_at          TIMESTAMPTZ    NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_chat_rooms_type ON chat_rooms(room_type, is_active);
CREATE INDEX idx_chat_rooms_uuid ON chat_rooms(id);

COMMENT ON TABLE chat_rooms IS 'Phòng chat: public, topic (theo chủ đề thời trang), direct (2 người).';


CREATE TABLE chat_room_members (
    room_internal_id INT         NOT NULL REFERENCES chat_rooms(internal_id) ON DELETE CASCADE,
    user_internal_id INT         NOT NULL REFERENCES users(internal_id)       ON DELETE CASCADE,
    joined_at        TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    last_read_at     TIMESTAMPTZ,
    is_muted         BOOLEAN     NOT NULL DEFAULT FALSE,
    PRIMARY KEY (room_internal_id, user_internal_id)
);

CREATE INDEX idx_chat_members_user ON chat_room_members(user_internal_id);

COMMENT ON TABLE chat_room_members IS 'Thành viên phòng chat. last_read_at dùng hiển thị số tin chưa đọc.';


-- BIGSERIAL vì volume tin nhắn rất lớn
CREATE TABLE chat_messages (
    internal_id        BIGSERIAL    PRIMARY KEY,
    id                 UUID         NOT NULL UNIQUE DEFAULT gen_random_uuid(),
    room_internal_id   INT          NOT NULL REFERENCES chat_rooms(internal_id)    ON DELETE CASCADE,
    user_internal_id   INT          NOT NULL REFERENCES users(internal_id)          ON DELETE CASCADE,
    content            TEXT,
    message_type       message_type NOT NULL DEFAULT 'text',
    outfit_internal_id INT          REFERENCES canvas_outfits(internal_id)          ON DELETE SET NULL,
    image_url          TEXT,
    deleted_at         TIMESTAMPTZ,
    sent_at            TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_messages_room ON chat_messages(room_internal_id, sent_at DESC);
CREATE INDEX idx_messages_user ON chat_messages(user_internal_id, sent_at DESC);

COMMENT ON TABLE chat_messages IS 'Lịch sử tin nhắn. Share outfit vào chat. Soft delete để moderator kiểm duyệt.';


-- =============================================================================
-- PHẦN 9: NOTIFICATIONS
-- =============================================================================

-- BIGSERIAL vì volume thông báo rất lớn
CREATE TABLE notifications (
    internal_id      BIGSERIAL   PRIMARY KEY,
    id               UUID        NOT NULL UNIQUE DEFAULT gen_random_uuid(),
    user_internal_id INT         NOT NULL REFERENCES users(internal_id) ON DELETE CASCADE,
    type             VARCHAR(50) NOT NULL,
    title            VARCHAR(255) NOT NULL,
    body             TEXT,
    reference_type   VARCHAR(50),
    reference_id     INT,                     -- internal_id của object liên quan
    is_read          BOOLEAN     NOT NULL DEFAULT FALSE,
    created_at       TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_notifications_user   ON notifications(user_internal_id, is_read, created_at DESC);
CREATE INDEX idx_notifications_unread ON notifications(user_internal_id) WHERE is_read = FALSE;

COMMENT ON TABLE notifications IS 'Thông báo in-app. reference_id là internal_id của object liên quan.';


-- =============================================================================
-- PHẦN 10: TRIGGERS — updated_at tự động
-- =============================================================================

CREATE OR REPLACE FUNCTION set_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_users_updated_at
    BEFORE UPDATE ON users FOR EACH ROW EXECUTE FUNCTION set_updated_at();

CREATE TRIGGER trg_customer_profiles_updated_at
    BEFORE UPDATE ON customer_profiles FOR EACH ROW EXECUTE FUNCTION set_updated_at();

CREATE TRIGGER trg_wardrobe_items_updated_at
    BEFORE UPDATE ON wardrobe_items FOR EACH ROW EXECUTE FUNCTION set_updated_at();

CREATE TRIGGER trg_canvas_outfits_updated_at
    BEFORE UPDATE ON canvas_outfits FOR EACH ROW EXECUTE FUNCTION set_updated_at();

CREATE TRIGGER trg_community_posts_updated_at
    BEFORE UPDATE ON community_posts FOR EACH ROW EXECUTE FUNCTION set_updated_at();


-- =============================================================================
-- PHẦN 11: TRIGGERS — cache count đồng bộ
-- Tránh COUNT(*) mỗi request, trigger giữ các cột cache luôn đúng
-- =============================================================================

-- wardrobe_item_count trong customer_profiles
CREATE OR REPLACE FUNCTION sync_wardrobe_count()
RETURNS TRIGGER AS $$
BEGIN
    IF TG_OP = 'INSERT' THEN
        UPDATE customer_profiles
        SET wardrobe_item_count = wardrobe_item_count + 1
        WHERE user_internal_id = NEW.user_internal_id;
    ELSIF TG_OP = 'DELETE' THEN
        UPDATE customer_profiles
        SET wardrobe_item_count = wardrobe_item_count - 1
        WHERE user_internal_id = OLD.user_internal_id;
    END IF;
    RETURN NULL;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_wardrobe_count
    AFTER INSERT OR DELETE ON wardrobe_items
    FOR EACH ROW EXECUTE FUNCTION sync_wardrobe_count();


-- like_count trong community_posts
CREATE OR REPLACE FUNCTION sync_post_like_count()
RETURNS TRIGGER AS $$
BEGIN
    IF TG_OP = 'INSERT' THEN
        UPDATE community_posts SET like_count = like_count + 1
        WHERE internal_id = NEW.post_internal_id;
    ELSIF TG_OP = 'DELETE' THEN
        UPDATE community_posts SET like_count = like_count - 1
        WHERE internal_id = OLD.post_internal_id;
    END IF;
    RETURN NULL;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_post_like_count
    AFTER INSERT OR DELETE ON post_likes
    FOR EACH ROW EXECUTE FUNCTION sync_post_like_count();


-- comment_count trong community_posts
CREATE OR REPLACE FUNCTION sync_post_comment_count()
RETURNS TRIGGER AS $$
BEGIN
    IF TG_OP = 'INSERT' THEN
        UPDATE community_posts SET comment_count = comment_count + 1
        WHERE internal_id = NEW.post_internal_id;
    ELSIF TG_OP = 'DELETE' THEN
        UPDATE community_posts SET comment_count = comment_count - 1
        WHERE internal_id = OLD.post_internal_id;
    END IF;
    RETURN NULL;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_post_comment_count
    AFTER INSERT OR DELETE ON post_comments
    FOR EACH ROW EXECUTE FUNCTION sync_post_comment_count();


-- =============================================================================
-- END OF SCHEMA v2.0
-- Tổng: 29 bảng, 10 enum types, 6 triggers, ~45 indexes
--
-- Thứ tự tạo bảng (dependency order):
--   1.  ENUMs
--   2.  users
--   3.  permission_levels → permissions → permission_level_defaults
--   4.  customer_profiles, admin_profiles, admin_permissions, brand_profiles
--   5.  refresh_tokens
--   6.  affiliate_products              <- phải trước canvas_outfit_items
--   7.  wardrobe_items, canvas_outfits, canvas_outfit_items, ai_lookbooks
--   8.  affiliate_clicks, affiliate_conversions
--   9.  sponsored_campaigns, campaign_impressions
--   10. premium_subscriptions
--   11. community_posts, post_comments, post_likes, post_reports, user_ban_logs
--   12. chat_rooms, chat_room_members, chat_messages
--   13. notifications
--
-- NGUYÊN TẮC KHÓA v2.0:
--   • Mọi FK đều trỏ đến internal_id (INT/SERIAL) — nhẹ, nhanh, tối ưu JOIN
--   • UUID chỉ dùng khi expose ra ngoài qua API/URL
--   • Flow API: nhận UUID từ request → SELECT internal_id WHERE id = $uuid
--                                     → dùng internal_id cho mọi query tiếp theo
-- =============================================================================
