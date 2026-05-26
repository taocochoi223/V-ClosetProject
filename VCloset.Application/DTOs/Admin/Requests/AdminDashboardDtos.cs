using System;

namespace VCloset.Application.DTOs.Admin.Requests;

public class DashboardMetricsResponse
{
    public int TotalUserCount { get; set; }
    public int PendingBrandCount { get; set; }
    public int PendingReportCount { get; set; }
    public decimal TotalSystemAdCredits { get; set; }
    public int ActivePremiumSubscriptionCount { get; set; }
    public decimal TotalPremiumRevenue { get; set; }
}

public class RevenueChartPoint
{
    public string TimeLabel { get; set; } = null!;
    public decimal Revenue { get; set; }
}
