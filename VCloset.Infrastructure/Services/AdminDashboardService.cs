using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VCloset.Application.DTOs.Admin.Requests;
using VCloset.Application.Interfaces;
using VCloset.Domain.Enums;
using VCloset.Infrastructure.Data;

namespace VCloset.Infrastructure.Services;

public class AdminDashboardService : IAdminDashboardService
{
    private readonly VClosetVersion30Context _context;

    public AdminDashboardService(VClosetVersion30Context context)
    {
        _context = context;
    }

    public async Task<DashboardMetricsResponse> GetDashboardMetricsAsync()
    {
        var totalUserCount = await _context.Users.CountAsync(u => u.Role == UserRole.Customer);
        var pendingBrandCount = await _context.BrandProfiles.CountAsync(bp => bp.Status == BrandStatus.Pending);
        var pendingReportCount = await _context.PostReports.CountAsync(pr => !pr.IsResolved);
        
        var totalSystemAdCredits = await _context.BrandProfiles.SumAsync(bp => bp.CreditBalance);
        
        var now = DateTime.UtcNow;
        var activePremiumSubscriptionCount = await _context.PremiumSubscriptions
            .CountAsync(ps => ps.IsActive && ps.ExpiresAt > now);
            
        var totalPremiumRevenue = await _context.PremiumSubscriptions.SumAsync(ps => ps.PricePaid);

        return new DashboardMetricsResponse
        {
            TotalUserCount = totalUserCount,
            PendingBrandCount = pendingBrandCount,
            PendingReportCount = pendingReportCount,
            TotalSystemAdCredits = totalSystemAdCredits,
            ActivePremiumSubscriptionCount = activePremiumSubscriptionCount,
            TotalPremiumRevenue = totalPremiumRevenue
        };
    }

    public async Task<List<RevenueChartPoint>> GetRevenueChartDataAsync(string period)
    {
        var points = new List<RevenueChartPoint>();
        var now = DateTime.UtcNow;

        if (string.Equals(period, "week", StringComparison.OrdinalIgnoreCase))
        {
            // Lấy 8 tuần gần nhất
            var today = now.Date;
            int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
            var currentWeekMonday = today.AddDays(-diff);
            var startLimit = currentWeekMonday.AddDays(-7 * 7); // 8 tuần tổng cộng

            var subscriptions = await _context.PremiumSubscriptions
                .Where(ps => ps.StartedAt >= startLimit)
                .Select(ps => new { ps.StartedAt, ps.PricePaid })
                .ToListAsync();

            for (int i = 7; i >= 0; i--)
            {
                var weekMonday = currentWeekMonday.AddDays(-7 * i);
                var weekSunday = weekMonday.AddDays(6).AddHours(23).AddMinutes(59).AddSeconds(59);
                var label = $"{weekMonday:dd/MM} - {weekSunday:dd/MM}";

                var revenue = subscriptions
                    .Where(s => s.StartedAt >= weekMonday && s.StartedAt <= weekSunday)
                    .Sum(s => s.PricePaid);

                points.Add(new RevenueChartPoint
                {
                    TimeLabel = label,
                    Revenue = revenue
                });
            }
        }
        else
        {
            // Mặc định: Lấy 6 tháng gần nhất
            var sixMonthsAgo = now.AddMonths(-5);
            var startOfMonthLimit = new DateTime(sixMonthsAgo.Year, sixMonthsAgo.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            var subscriptions = await _context.PremiumSubscriptions
                .Where(ps => ps.StartedAt >= startOfMonthLimit)
                .Select(ps => new { ps.StartedAt, ps.PricePaid })
                .ToListAsync();

            for (int i = 5; i >= 0; i--)
            {
                var targetMonth = now.AddMonths(-i);
                var label = $"Tháng {targetMonth:MM/yyyy}";

                var revenue = subscriptions
                    .Where(s => s.StartedAt.Year == targetMonth.Year && s.StartedAt.Month == targetMonth.Month)
                    .Sum(s => s.PricePaid);

                points.Add(new RevenueChartPoint
                {
                    TimeLabel = label,
                    Revenue = revenue
                });
            }
        }

        return points;
    }
}
