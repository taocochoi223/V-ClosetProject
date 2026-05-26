using System.Collections.Generic;
using System.Threading.Tasks;
using VCloset.Application.DTOs.Admin.Requests;

namespace VCloset.Application.Interfaces;

public interface IAdminDashboardService
{
    Task<DashboardMetricsResponse> GetDashboardMetricsAsync();
    Task<List<RevenueChartPoint>> GetRevenueChartDataAsync(string period); // "week" hoặc "month"
}
