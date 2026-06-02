using System.Collections.Generic;
using System.Threading.Tasks;
using VCloset.Application.DTOs.Admin.Requests;

namespace VCloset.Application.Interfaces;

public interface IAdminDashboardService
{
    // 1. KPI metrics card (4 thẻ trên cùng)
    Task<DashboardMetricsResponse> GetDashboardMetricsAsync();

    // 2. Biểu đồ doanh thu Premium vs Hoa hồng Affiliate theo thời gian
    Task<List<RevenueChartPoint>> GetRevenueChartDataAsync(string period); // "week" hoặc "month"

    // 3. Danh sách người dùng mới đăng ký gần đây
    Task<List<RecentSignupResponse>> GetRecentSignupsAsync(int limit = 5);

    // 4. Bảng tin hệ thống & Cảnh báo API
    Task<List<SystemAlertResponse>> GetSystemAlertsAsync();

    // 5. Xuất báo cáo tổng hợp ra file CSV
    Task<byte[]> ExportDashboardReportAsync(DateTime? from, DateTime? to);
}

