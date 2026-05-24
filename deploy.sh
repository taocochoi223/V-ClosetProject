#!/bin/bash
# ============================================================
# 🔵🟢 Blue-Green Zero-Downtime Deploy Script
# Cách dùng: ./deploy.sh
# ============================================================

set -e  # Thoát ngay nếu có lệnh nào lỗi

PROJECT_DIR="$(cd "$(dirname "$0")" && pwd)"
NGINX_CONF="$PROJECT_DIR/nginx/default.conf"

echo "========================================"
echo "🚀 V-Closet Blue-Green Deploy"
echo "========================================"

# ── Bước 1: Xác định slot nào đang active ──────────────────
if docker ps --format '{{.Names}}' | grep -q "vcloset-api-blue"; then
    ACTIVE="blue"
    INACTIVE="green"
    ACTIVE_PORT="8080"
    INACTIVE_INTERNAL="vcloset-api-green:8080"
else
    ACTIVE="green"
    INACTIVE="blue"
    ACTIVE_PORT="8080"
    INACTIVE_INTERNAL="vcloset-api-blue:8080"
fi

echo "📍 Slot đang chạy: $ACTIVE"
echo "🎯 Slot sẽ deploy lên: $INACTIVE"

# ── Bước 2: Build image mới vào slot INACTIVE ──────────────
echo ""
echo "🔨 [1/5] Building image mới cho slot '$INACTIVE'..."
docker build -f VCloset.API/Dockerfile -t "vcloset-api:$INACTIVE" .

# ── Bước 3: Start container INACTIVE ───────────────────────
echo ""
echo "🟢 [2/5] Khởi động container '$INACTIVE'..."
docker compose up -d --no-deps "api-$INACTIVE"

# ── Bước 4: Health check container INACTIVE ────────────────
echo ""
echo "❤️  [3/5] Health check container '$INACTIVE'..."
HEALTH_URL="http://localhost:8080/health"
RETRIES=12   # 12 x 5s = 60s timeout
COUNT=0

# Lấy port tạm của container inactive để check từ host
INACTIVE_PORT=$(docker inspect --format='{{(index (index .NetworkSettings.Ports "8080/tcp") 0).HostPort}}' "vcloset-api-$INACTIVE" 2>/dev/null || echo "")

while [ $COUNT -lt $RETRIES ]; do
    # Check health qua docker healthcheck status
    HEALTH=$(docker inspect --format='{{.State.Health.Status}}' "vcloset-api-$INACTIVE" 2>/dev/null || echo "starting")
    
    if [ "$HEALTH" = "healthy" ]; then
        echo "✅ Container '$INACTIVE' healthy!"
        break
    fi
    
    COUNT=$((COUNT + 1))
    echo "  ⏳ [$COUNT/$RETRIES] Đang chờ... (status: $HEALTH)"
    sleep 5
done

if [ $COUNT -eq $RETRIES ]; then
    echo "❌ Health check thất bại sau 60 giây! Rollback..."
    echo "📋 Logs của container '$INACTIVE':"
    docker logs "vcloset-api-$INACTIVE" --tail 100
    docker compose stop "api-$INACTIVE"
    exit 1
fi

# ── Bước 5: Switch Nginx sang INACTIVE (traffic chuyển ngay) ─
echo ""
echo "🔀 [4/5] Chuyển Nginx traffic sang '$INACTIVE'..."
cat > "$NGINX_CONF" << EOF
upstream vcloset_active {
    server vcloset-api-${INACTIVE}:8080;
}

server {
    listen 80;

    location / {
        proxy_pass         http://vcloset_active;
        proxy_http_version 1.1;
        proxy_set_header   Upgrade \$http_upgrade;
        proxy_set_header   Connection keep-alive;
        proxy_set_header   Host \$host;
        proxy_set_header   X-Real-IP \$remote_addr;
        proxy_set_header   X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header   X-Forwarded-Proto \$scheme;
        proxy_cache_bypass \$http_upgrade;

        proxy_read_timeout 120s;
        proxy_send_timeout 120s;
    }
}
EOF

# Reload Nginx — KHÔNG restart, chỉ reload config (0 downtime)
docker exec vcloset-nginx nginx -s reload
echo "✅ Nginx đã chuyển sang '$INACTIVE'!"

# ── Bước 6: Stop container cũ (ACTIVE) ─────────────────────
echo ""
echo "🛑 [5/5] Dừng container cũ '$ACTIVE'..."
docker compose stop "api-$ACTIVE"

# ── Dọn dẹp ─────────────────────────────────────────────────
docker image prune -f > /dev/null 2>&1

echo ""
echo "========================================"
echo "✅ Deploy thành công! Zero downtime!"
echo "   Slot active mới: $INACTIVE"
echo "========================================"
