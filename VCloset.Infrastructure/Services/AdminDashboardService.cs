using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VCloset.Application.DTOs.Admin.Requests;
using VCloset.Application.DTOs.Admin.Responses;
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

    // 1. Lấy toàn bộ KPI Metrics phù hợp với 4 thẻ trên UI
    public async Task<DashboardMetricsResponse> GetDashboardMetricsAsync()
    {
        var now = DateTime.UtcNow;
        var last24h = now.AddHours(-24);
        var startOfThisMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var startOfLastMonth = startOfThisMonth.AddMonths(-1);

        // KPI Card 1: Tổng người dùng (Tất cả người dùng từ đầu đến giờ, bao gồm active/inactive, các role)
        var totalUserCount = await _context.Users.CountAsync();
        var newUsersLast24h = await _context.Users.CountAsync(u => u.CreatedAt >= last24h);

        // KPI Card 2: Doanh thu Premium
        var totalPremiumRevenue = await _context.PremiumSubscriptions.SumAsync(ps => ps.PricePaid);
        var revenueThisMonth = await _context.PremiumSubscriptions
            .Where(ps => ps.StartedAt >= startOfThisMonth)
            .SumAsync(ps => ps.PricePaid);
        var revenueLastMonth = await _context.PremiumSubscriptions
            .Where(ps => ps.StartedAt >= startOfLastMonth && ps.StartedAt < startOfThisMonth)
            .SumAsync(ps => ps.PricePaid);
        var premiumGrowthPercent = revenueLastMonth > 0
            ? Math.Round((double)((revenueThisMonth - revenueLastMonth) / revenueLastMonth * 100), 1)
            : 0;

        // KPI Card 3: Hoa hồng Shopee Affiliate
        var totalAffiliateCommission = await _context.AffiliateConversions.SumAsync(ac => ac.CommissionAmount);
        var clicksThisMonth = await _context.AffiliateClicks.CountAsync(c => c.ClickedAt >= startOfThisMonth);
        var clicksLastMonth = await _context.AffiliateClicks.CountAsync(c => c.ClickedAt >= startOfLastMonth && c.ClickedAt < startOfThisMonth);
        var affiliateClickGrowthPercent = clicksLastMonth > 0
            ? Math.Round((double)((clicksThisMonth - clicksLastMonth) / (double)clicksLastMonth * 100), 1)
            : 0;

        // KPI Card 4: Chi phí API AI (tính từ tổng AI Lookbook + tách nền - ước tính chi phí/request)
        var aiLookbookThisMonth = await _context.AiLookbooks.CountAsync(a => a.CreatedAt >= startOfThisMonth);
        var aiLookbookLastMonth = await _context.AiLookbooks.CountAsync(a => a.CreatedAt >= startOfLastMonth && a.CreatedAt < startOfThisMonth);
        const decimal CostPerAiRequest = 0.05m; // $0.05/request ước tính
        var totalApiAiCost = (await _context.AiLookbooks.CountAsync()) * CostPerAiRequest;
        var aiLookbookGrowthPercent = aiLookbookLastMonth > 0
            ? Math.Round((double)((aiLookbookThisMonth - aiLookbookLastMonth) / (double)aiLookbookLastMonth * 100), 1)
            : 0;

        // Chỉ số phụ
        var pendingBrandCount = await _context.BrandProfiles.CountAsync(bp => bp.Status == BrandStatus.Pending);
        var pendingReportCount = await _context.PostReports.CountAsync(pr => !pr.IsResolved);
        var totalSystemAdCredits = await _context.BrandProfiles.SumAsync(bp => bp.CreditBalance);
        var activePremiumSubscriptionCount = await _context.PremiumSubscriptions
            .CountAsync(ps => ps.IsActive && ps.ExpiresAt > now);

        return new DashboardMetricsResponse
        {
            TotalUserCount = totalUserCount,
            NewUsersLast24h = newUsersLast24h,
            TotalPremiumRevenue = totalPremiumRevenue,
            PremiumRevenueGrowthPercent = (decimal)premiumGrowthPercent,
            TotalAffiliateCommission = totalAffiliateCommission,
            AffiliateClickGrowthPercent = (decimal)affiliateClickGrowthPercent,
            TotalApiAiCost = totalApiAiCost,
            ApiAiCostGrowthPercent = (decimal)aiLookbookGrowthPercent,
            PendingBrandCount = pendingBrandCount,
            PendingReportCount = pendingReportCount,
            TotalSystemAdCredits = totalSystemAdCredits,
            ActivePremiumSubscriptionCount = activePremiumSubscriptionCount
        };
    }

    // 2. Biểu đồ doanh thu Premium vs Hoa hồng Affiliate theo thời gian (6 tháng / 8 tuần)
    public async Task<List<RevenueChartPoint>> GetRevenueChartDataAsync(string period)
    {
        var points = new List<RevenueChartPoint>();
        var now = DateTime.UtcNow;

        if (string.Equals(period, "week", StringComparison.OrdinalIgnoreCase))
        {
            var today = now.Date;
            int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
            var currentWeekMonday = today.AddDays(-diff);
            var startLimit = currentWeekMonday.AddDays(-7 * 7);

            var subscriptions = await _context.PremiumSubscriptions
                .Where(ps => ps.StartedAt >= startLimit)
                .Select(ps => new { ps.StartedAt, ps.PricePaid })
                .ToListAsync();

            var conversions = await _context.AffiliateConversions
                .Where(ac => ac.ConvertedAt >= startLimit)
                .Select(ac => new { ac.ConvertedAt, ac.CommissionAmount })
                .ToListAsync();

            for (int i = 7; i >= 0; i--)
            {
                var weekMonday = currentWeekMonday.AddDays(-7 * i);
                var weekSunday = weekMonday.AddDays(6).AddHours(23).AddMinutes(59).AddSeconds(59);
                var label = $"{weekMonday:dd/MM} - {weekSunday:dd/MM}";

                var revenue = subscriptions
                    .Where(s => s.StartedAt >= weekMonday && s.StartedAt <= weekSunday)
                    .Sum(s => s.PricePaid);

                var commission = conversions
                    .Where(c => c.ConvertedAt >= weekMonday && c.ConvertedAt <= weekSunday)
                    .Sum(c => c.CommissionAmount);

                points.Add(new RevenueChartPoint
                {
                    TimeLabel = label,
                    Revenue = revenue,
                    AffiliateCommission = commission
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

            var conversions = await _context.AffiliateConversions
                .Where(ac => ac.ConvertedAt >= startOfMonthLimit)
                .Select(ac => new { ac.ConvertedAt, ac.CommissionAmount })
                .ToListAsync();

            for (int i = 5; i >= 0; i--)
            {
                var targetMonth = now.AddMonths(-i);
                var label = $"Tháng {targetMonth.Month}";

                var revenue = subscriptions
                    .Where(s => s.StartedAt.Year == targetMonth.Year && s.StartedAt.Month == targetMonth.Month)
                    .Sum(s => s.PricePaid);

                var commission = conversions
                    .Where(c => c.ConvertedAt.Year == targetMonth.Year && c.ConvertedAt.Month == targetMonth.Month)
                    .Sum(c => c.CommissionAmount);

                points.Add(new RevenueChartPoint
                {
                    TimeLabel = label,
                    Revenue = revenue,
                    AffiliateCommission = commission
                });
            }
        }

        return points;
    }

    // 3. Danh sách người dùng mới đăng ký gần đây (N người mới nhất, không giới hạn thời gian)
    public async Task<List<RecentSignupResponse>> GetRecentSignupsAsync(int limit = 8)
    {
        return await _context.Users
            .OrderByDescending(u => u.CreatedAt)
            .Take(limit)
            .Select(u => new RecentSignupResponse
            {
                UserId = u.Id,
                DisplayName = u.DisplayName,
                Email = u.Email,
                AvatarUrl = u.AvatarUrl,
                Role = u.Role.ToString(),
                CreatedAt = u.CreatedAt
            })
            .ToListAsync();
    }

    // 4. Bảng tin hệ thống & Cảnh báo API (tổng hợp từ nhiều nguồn dữ liệu thực)
    public async Task<List<SystemAlertResponse>> GetSystemAlertsAsync()
    {
        var alerts = new List<SystemAlertResponse>();
        var now = DateTime.UtcNow;

        // Cảnh báo 1: Số dư tín dụng Brand thấp (dưới 100.000 VND)
        const decimal CreditWarningThreshold = 100_000m;
        var lowCreditBrands = await _context.BrandProfiles
            .Where(bp => bp.CreditBalance < CreditWarningThreshold && bp.Status == BrandStatus.Verified)
            .CountAsync();
        if (lowCreditBrands > 0)
        {
            alerts.Add(new SystemAlertResponse
            {
                Type = "warning",
                Message = $"{lowCreditBrands} thương hiệu Partner có số dư tín dụng quảng cáo thấp (dưới 100.000 VNĐ). Vui lòng kiểm tra và nhắc nhở nạp tiền.",
                CreatedAt = now
            });
        }

        // Thông báo 2: Giao dịch Premium thành công trong 24h gần đây
        var last24h = now.AddHours(-24);
        var recentPremiumTxCount = await _context.PremiumSubscriptions
            .Where(ps => ps.StartedAt >= last24h)
            .CountAsync();
        if (recentPremiumTxCount > 0)
        {
            var totalRevenue24h = await _context.PremiumSubscriptions
                .Where(ps => ps.StartedAt >= last24h)
                .SumAsync(ps => ps.PricePaid);
            alerts.Add(new SystemAlertResponse
            {
                Type = "success",
                Message = $"{recentPremiumTxCount} giao dịch Premium thành công trong 24h qua. Tổng doanh thu: ${totalRevenue24h:N2}.",
                CreatedAt = now.AddHours(-1)
            });
        }

        // Thông báo 3: Báo cáo nội dung chưa xử lý
        var pendingReportCount = await _context.PostReports.CountAsync(pr => !pr.IsResolved);
        if (pendingReportCount > 0)
        {
            alerts.Add(new SystemAlertResponse
            {
                Type = "warning",
                Message = $"Có {pendingReportCount} báo cáo nội dung vi phạm chưa được xử lý. Vui lòng kiểm tra trang Kiểm duyệt.",
                CreatedAt = now.AddHours(-2)
            });
        }

        // Thông báo 4: Brand Partner mới chờ duyệt
        var pendingBrandCount = await _context.BrandProfiles.CountAsync(bp => bp.Status == BrandStatus.Pending);
        if (pendingBrandCount > 0)
        {
            alerts.Add(new SystemAlertResponse
            {
                Type = "info",
                Message = $"{pendingBrandCount} đối tác thương hiệu mới đang chờ duyệt hồ sơ. Vui lòng kiểm tra trang Brands.",
                CreatedAt = now.AddHours(-3)
            });
        }

        // Thông báo 5: Affiliate Conversions mới trong 24h
        var newConversions24h = await _context.AffiliateConversions
            .Where(ac => ac.ConvertedAt >= last24h)
            .CountAsync();
        if (newConversions24h > 0)
        {
            var commissionTotal = await _context.AffiliateConversions
                .Where(ac => ac.ConvertedAt >= last24h)
                .SumAsync(ac => ac.CommissionAmount);
            alerts.Add(new SystemAlertResponse
            {
                Type = "success",
                Message = $"Crawler Affiliate ghi nhận {newConversions24h} đơn hàng Shopee thành công trong 24h. Hoa hồng: ${commissionTotal:N2}.",
                CreatedAt = now.AddHours(-2)
            });
        }

        // Sắp xếp theo thời gian mới nhất lên đầu
        return alerts.OrderByDescending(a => a.CreatedAt).ToList();
    }

    // 5. Xuất báo cáo tổng hợp CSV
    public async Task<byte[]> ExportDashboardReportAsync(DateTime? from, DateTime? to)
    {
        var fromDate = from ?? DateTime.UtcNow.AddMonths(-1);
        var toDate = to ?? DateTime.UtcNow;

        var subscriptions = await _context.PremiumSubscriptions
            .Where(ps => ps.StartedAt >= fromDate && ps.StartedAt <= toDate)
            .Include(ps => ps.UserInternal)
            .ToListAsync();

        var conversions = await _context.AffiliateConversions
            .Where(ac => ac.ConvertedAt >= fromDate && ac.ConvertedAt <= toDate)
            .ToListAsync();

        var csv = new StringBuilder();
        csv.AppendLine("Loại,Mô tả,Ngày,Số tiền (USD)");

        foreach (var ps in subscriptions)
        {
            csv.AppendLine($"\"Premium\",\"Gói {ps.PlanType} - {ps.UserInternal?.Email ?? "N/A"}\",\"{ps.StartedAt:yyyy-MM-dd HH:mm:ss}\",{ps.PricePaid}");
        }

        foreach (var conv in conversions)
        {
            csv.AppendLine($"\"Affiliate\",\"Đơn hàng Shopee #{conv.ShopeeOrderId ?? "N/A"}\",\"{conv.ConvertedAt:yyyy-MM-dd HH:mm:ss}\",{conv.CommissionAmount}");
        }

        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
    }

    // 6. Phân tích nhân khẩu học dựa trên khảo sát Onboarding
    public async Task<OnboardingDemographicsDto> GetOnboardingDemographicsAsync()
    {
        var dto = new OnboardingDemographicsDto();

        // Lấy tất cả CustomerProfiles đã hoàn thành Onboarding
        var completedProfiles = await _context.CustomerProfiles
            .Where(cp => cp.IsOnboardingCompleted)
            .Select(cp => new
            {
                cp.BodyShape,
                cp.Lifestyle,
                cp.EyeColor,
                cp.Hair,
                cp.Gender,
                cp.DateOfBirth
            })
            .ToListAsync();

        dto.TotalCompletedOnboarding = completedProfiles.Count;

        if (dto.TotalCompletedOnboarding == 0)
        {
            return dto;
        }

        // Group BodyShapes
        dto.BodyShapes = completedProfiles
            .Where(x => x.BodyShape.HasValue)
            .GroupBy(x => x.BodyShape!.Value.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        // Group Lifestyles
        dto.Lifestyles = completedProfiles
            .Where(x => !string.IsNullOrEmpty(x.Lifestyle))
            .GroupBy(x => x.Lifestyle!)
            .ToDictionary(g => g.Key, g => g.Count());

        // Group EyeColors
        dto.EyeColors = completedProfiles
            .Where(x => !string.IsNullOrEmpty(x.EyeColor))
            .GroupBy(x => x.EyeColor!)
            .ToDictionary(g => g.Key, g => g.Count());

        // Group HairColors
        dto.HairColors = completedProfiles
            .Where(x => !string.IsNullOrEmpty(x.Hair))
            .GroupBy(x => x.Hair!)
            .ToDictionary(g => g.Key, g => g.Count());

        // Group Genders
        dto.Genders = completedProfiles
            .Where(x => !string.IsNullOrEmpty(x.Gender))
            .GroupBy(x => x.Gender!)
            .ToDictionary(g => g.Key, g => g.Count());

        // Group Age
        var now = DateTime.UtcNow;
        foreach (var p in completedProfiles)
        {
            if (p.DateOfBirth.HasValue)
            {
                int age = now.Year - p.DateOfBirth.Value.Year;
                if (p.DateOfBirth.Value.Date > now.AddYears(-age)) age--;

                string ageGroup = "Unknown";
                if (age < 18) ageGroup = "< 18";
                else if (age <= 24) ageGroup = "18 - 24";
                else if (age <= 34) ageGroup = "25 - 34";
                else if (age <= 44) ageGroup = "35 - 44";
                else ageGroup = "45+";

                if (!dto.AgeGroups.ContainsKey(ageGroup)) dto.AgeGroups[ageGroup] = 0;
                dto.AgeGroups[ageGroup]++;
            }
        }

        return dto;
    }
}

