namespace VCloset.Application.DTOs.Subscriptions.Responses;

public class PremiumSubscriptionStatsResponse
{
    // Doanh thu tháng này
    public decimal CurrentMonthRevenue { get; set; }
    public double RevenuePercentageChange { get; set; }

    // Đăng ký mới (30 ngày)
    public int NewSubscriptions { get; set; }
    public double NewSubscriptionsPercentageChange { get; set; }

    // Tỷ lệ hủy (Churn rate)
    public double ChurnRate { get; set; }
    public double ChurnRatePercentageChange { get; set; }
}
