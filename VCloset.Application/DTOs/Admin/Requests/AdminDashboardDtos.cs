using System;
using System.Collections.Generic;

namespace VCloset.Application.DTOs.Admin.Requests;

public class DashboardMetricsResponse
{
    // KPI Card 1: Tổng người dùng
    public int TotalUserCount { get; set; }
    public int NewUsersLast24h { get; set; }

    // KPI Card 2: Doanh thu Premium
    public decimal TotalPremiumRevenue { get; set; }
    public decimal PremiumRevenueGrowthPercent { get; set; } // % so với tháng trước

    // KPI Card 3: Hoa hồng Shopee (Affiliate)
    public decimal TotalAffiliateCommission { get; set; }
    public decimal AffiliateClickGrowthPercent { get; set; } // % tăng lượt nhấn Canvas

    // KPI Card 4: Chi phí API AI (Photoroom/FASHN)
    public decimal TotalApiAiCost { get; set; }           // mock từ tổng impression AI Lookbook & tách nền
    public decimal ApiAiCostGrowthPercent { get; set; }   // % tăng AI Lookbook

    // Các chỉ số phụ
    public int PendingBrandCount { get; set; }
    public int PendingReportCount { get; set; }
    public decimal TotalSystemAdCredits { get; set; }
    public int ActivePremiumSubscriptionCount { get; set; }
}

public class RevenueChartPoint
{
    public string TimeLabel { get; set; } = null!;
    public decimal Revenue { get; set; }
    public decimal AffiliateCommission { get; set; } // Hoa hồng Shopee cùng kỳ
}

// Đăng ký mới gần đây
public class RecentSignupResponse
{
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? AvatarUrl { get; set; }
    public string Role { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}

// Bảng tin hệ thống & cảnh báo API
public class SystemAlertResponse
{
    public string Type { get; set; } = null!;   // "warning" | "success" | "info"
    public string Message { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}

// Export báo cáo dashboard
public class DashboardExportRequest
{
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}

