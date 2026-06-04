using System;
using VCloset.Domain.Enums;

namespace VCloset.Application.DTOs.Admin.Requests;

// Request body khi Admin duyệt hoặc đình chỉ đối tác thương hiệu
public class UpdateBrandStatusRequest
{
    public BrandStatus Status { get; set; }
    public string? Notes { get; set; }
}

// Request body khi Admin nạp tiền quảng cáo (Credit) cho đối tác
public class RechargeBrandCreditRequest
{
    public decimal Amount { get; set; }
    public string? Description { get; set; }
}

// Response thông tin tóm tắt của Brand Partner hiển thị trên Admin Panel
public class BrandSummaryResponse
{
    public Guid BrandId { get; set; }
    public string BrandName { get; set; } = null!;
    public string? LogoUrl { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? ContactPhone { get; set; }
    public string? TaxCode { get; set; }
    public decimal CreditBalance { get; set; }
    public BrandStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Thông tin tài khoản User liên kết
    public Guid UserId { get; set; }
    public string UserEmail { get; set; } = null!;
    public string UserDisplayName { get; set; } = null!;
}

// Response thông tin chiến dịch quảng cáo được tài tài trợ
public class CampaignSummaryResponse
{
    public Guid CampaignId { get; set; }
    public string BrandName { get; set; } = null!;
    public string ProductName { get; set; } = null!;
    public string ProductImageUrl { get; set; } = null!;
    public short DisplayRank { get; set; }
    public decimal DailyBudget { get; set; }
    public decimal TotalSpent { get; set; }
    public int ImpressionCount { get; set; }
    public int ClickCount { get; set; }
    public bool IsActive { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public DateTime CreatedAt { get; set; }

    // Chỉ số hiệu suất CTR (%) tính toán tự động
    public double Ctr => ImpressionCount > 0 ? Math.Round((double)ClickCount / ImpressionCount * 100, 2) : 0.0;
}

// Request body khi Admin điều chỉnh ngân sách ngày và vị trí hiển thị chiến dịch quảng cáo
public class AdjustCampaignRequest
{
    public decimal DailyBudget { get; set; }
    public short DisplayRank { get; set; }
}

// Response phân trang cho chiến dịch quảng cáo
public class PagedCampaignsResponse
{
    public List<CampaignSummaryResponse> Campaigns { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

// Response số liệu thống kê tổng hợp (KPI Cards) cho chiến dịch quảng cáo tài trợ
public class CampaignDashboardMetricsResponse
{
    public int ActiveCampaignsCount { get; set; }       // Số chiến dịch đang chạy (IsActive = true)
    public int TotalCampaignsCount { get; set; }        // Tổng số chiến dịch trên hệ thống
    public decimal TotalDailyBudget { get; set; }       // Tổng ngân sách chạy hàng ngày của các chiến dịch đang chạy
    public decimal TotalSpent { get; set; }             // Doanh thu quảng cáo lũy kế từ các chiến dịch
    public int TotalImpressions { get; set; }           // Tổng lượt view (views/impressions)
    public int TotalClicks { get; set; }                // Tổng lượt click (clicks)
    public double OverallCtr { get; set; }              // Hiệu suất CTR chung (%)
}

// Request body khi Admin tạo một chiến dịch quảng cáo mới
public class CreateCampaignRequest
{
    public Guid BrandId { get; set; }
    public Guid ProductId { get; set; }
    public short DisplayRank { get; set; }
    public decimal DailyBudget { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
}

// Response thông tin tóm tắt sản phẩm để hiển thị trong ô chọn dropdown khi tạo chiến dịch
public class ProductSelectResponse
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = null!;
    public string ProductImageUrl { get; set; } = null!;
}



