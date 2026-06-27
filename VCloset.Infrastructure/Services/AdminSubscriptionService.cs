using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VCloset.Application.DTOs.Subscriptions.Requests;
using VCloset.Application.DTOs.Subscriptions.Responses;
using VCloset.Application.Interfaces;
using VCloset.Domain.Entities;
using VCloset.Domain.Enums;
using VCloset.Infrastructure.Data;

namespace VCloset.Infrastructure.Services;

public class AdminSubscriptionService : IAdminSubscriptionService
{
    private readonly VClosetVersion30Context _context;
    private readonly INotificationHubService _notificationHubService;
    private readonly INotificationService _notificationService;

    public AdminSubscriptionService(
        VClosetVersion30Context context,
        INotificationHubService notificationHubService,
        INotificationService notificationService)
    {
        _context = context;
        _notificationHubService = notificationHubService;
        _notificationService = notificationService;
    }

    public async Task<IEnumerable<SubscriptionPlanResponse>> GetAllPlansAsync()
    {
        return await _context.SubscriptionPlans
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new SubscriptionPlanResponse
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                Currency = p.Currency,
                DurationDays = p.DurationDays,
                GrantedBgCredits = p.GrantedBgCredits,
                GrantedTryOnCredits = p.GrantedTryOnCredits,
                IsActive = p.IsActive
            })
            .ToListAsync();
    }

    public async Task<SubscriptionPlanResponse> GetPlanByIdAsync(Guid planId)
    {
        var plan = await _context.SubscriptionPlans.FirstOrDefaultAsync(p => p.Id == planId);
        if (plan == null)
            throw new Exception("Không tìm thấy gói dịch vụ.");

        return new SubscriptionPlanResponse
        {
            Id = plan.Id,
            Name = plan.Name,
            Description = plan.Description,
            Price = plan.Price,
            Currency = plan.Currency,
            DurationDays = plan.DurationDays,
            GrantedBgCredits = plan.GrantedBgCredits,
            GrantedTryOnCredits = plan.GrantedTryOnCredits,
            IsActive = plan.IsActive
        };
    }

    public async Task<SubscriptionPlanResponse> CreatePlanAsync(CreateOrUpdatePlanRequest request)
    {
        var plan = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            Currency = request.Currency,
            DurationDays = request.DurationDays == 0 ? null : request.DurationDays,
            GrantedBgCredits = request.GrantedBgCredits,
            GrantedTryOnCredits = request.GrantedTryOnCredits,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.SubscriptionPlans.Add(plan);
        await _context.SaveChangesAsync();

        return new SubscriptionPlanResponse
        {
            Id = plan.Id,
            Name = plan.Name,
            Description = plan.Description,
            Price = plan.Price,
            Currency = plan.Currency,
            DurationDays = plan.DurationDays,
            GrantedBgCredits = plan.GrantedBgCredits,
            GrantedTryOnCredits = plan.GrantedTryOnCredits,
            IsActive = plan.IsActive
        };
    }

    public async Task<SubscriptionPlanResponse> UpdatePlanAsync(Guid planId, UpdatePlanRequest request)
    {
        var plan = await _context.SubscriptionPlans.FirstOrDefaultAsync(p => p.Id == planId);
        if (plan == null)
            throw new Exception("Không tìm thấy gói dịch vụ.");

        if (!string.IsNullOrWhiteSpace(request.Name)) plan.Name = request.Name;
        if (request.Description != null) plan.Description = request.Description; // Cho phép xoá mô tả bằng cách gửi chuỗi rỗng
        if (request.Price.HasValue) plan.Price = request.Price.Value;
        if (!string.IsNullOrWhiteSpace(request.Currency)) plan.Currency = request.Currency;
        if (request.DurationDays.HasValue) plan.DurationDays = request.DurationDays.Value == 0 ? null : request.DurationDays.Value;
        plan.GrantedBgCredits = request.GrantedBgCredits;
        plan.GrantedTryOnCredits = request.GrantedTryOnCredits;
        if (request.IsActive.HasValue) plan.IsActive = request.IsActive.Value;
        
        plan.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return new SubscriptionPlanResponse
        {
            Id = plan.Id,
            Name = plan.Name,
            Description = plan.Description,
            Price = plan.Price,
            Currency = plan.Currency,
            DurationDays = plan.DurationDays,
            GrantedBgCredits = plan.GrantedBgCredits,
            GrantedTryOnCredits = plan.GrantedTryOnCredits,
            IsActive = plan.IsActive
        };
    }

    public async Task<bool> DeletePlanAsync(Guid planId)
    {
        var plan = await _context.SubscriptionPlans.FirstOrDefaultAsync(p => p.Id == planId);
        if (plan == null)
            throw new Exception("Không tìm thấy gói dịch vụ.");

        // Soft delete bằng cách set IsActive = false để không ảnh hưởng đến dữ liệu cũ
        plan.IsActive = false;
        plan.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<PagedPremiumSubscriptionsResponse> GetPremiumSubscriptionsAsync(
        int page, int pageSize, string? search, bool? isActive, string? planType)
    {
        var query = _context.PremiumSubscriptions
            .Include(ps => ps.UserInternal)
            .Include(ps => ps.SubscriptionPlan)
            .AsQueryable();

        if (isActive.HasValue)
        {
            query = query.Where(ps => ps.IsActive == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(planType))
        {
            var pType = planType.ToLower() == "yearly" ? PremiumPlan.Yearly : PremiumPlan.Monthly;
            query = query.Where(ps => ps.PlanType == pType);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(ps => 
                ps.UserInternal.Email.ToLower().Contains(searchLower) ||
                ps.UserInternal.DisplayName.ToLower().Contains(searchLower) ||
                (ps.SubscriptionPlan != null && ps.SubscriptionPlan.Name.ToLower().Contains(searchLower))
            );
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(ps => ps.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ps => new PremiumSubscriptionListItem
            {
                SubscriptionId = ps.Id,
                UserId = ps.UserInternal.Id,
                Email = ps.UserInternal.Email,
                DisplayName = ps.UserInternal.DisplayName,
                PlanName = ps.SubscriptionPlan != null ? ps.SubscriptionPlan.Name : ps.PlanType.ToString(),
                PlanType = ps.PlanType.ToString().ToLower(),
                PricePaid = ps.PricePaid,
                Currency = ps.Currency,
                PaymentMethod = ps.PaymentMethod ?? "unknown",
                PaymentRef = ps.PaymentRef ?? "none",
                StartedAt = ps.StartedAt,
                ExpiresAt = ps.ExpiresAt,
                IsActive = ps.IsActive
            })
            .ToListAsync();

        return new PagedPremiumSubscriptionsResponse
        {
            Subscriptions = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<bool> RevokePremiumSubscriptionAsync(Guid subscriptionId, string? adminNote, int adminId)
    {
        var subscription = await _context.PremiumSubscriptions
            .FirstOrDefaultAsync(ps => ps.Id == subscriptionId);
        
        if (subscription == null)
            throw new Exception("Không tìm thấy gói Premium của người dùng.");

        if (!subscription.IsActive)
            throw new Exception("Gói Premium này hiện không hoạt động hoặc đã bị hủy/hết hạn trước đó.");

        subscription.IsActive = false;
        subscription.CancelledAt = DateTime.UtcNow;

        // Reset user credits to free tier configuration
        var profile = await _context.CustomerProfiles
            .FirstOrDefaultAsync(cp => cp.UserInternalId == subscription.UserInternalId);
        if (profile != null)
        {
            var freeTier = await _context.SubscriptionTierConfigs
                .FirstOrDefaultAsync(tc => tc.TierName.ToLower() == "free");
            if (freeTier != null)
            {
                profile.BgRemovalCredits = freeTier.BgRemovalCredits;
                profile.TryOnCredits = freeTier.TryOnCredits;
                profile.UpdatedAt = DateTime.UtcNow;
                _context.CustomerProfiles.Update(profile);
            }
        }
        
        await _context.SaveChangesAsync();

        // Lưu thông báo vào CSDL và gửi Real-time Notification qua SignalR
        try
        {
            await _notificationService.SendNotificationAsync(
                subscription.UserInternalId,
                "System",
                "Gói Premium bị thu hồi",
                $"Gói Premium của bạn đã bị thu hồi bởi quản trị viên. Lý do: {adminNote ?? "Không có lý do cụ thể"}",
                "PremiumSubscription",
                subscription.InternalId
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Database Notification Revoke Error]: {ex.Message}");
        }

        // Bắn SignalR báo về máy user
        try
        {
            await _notificationHubService.SendPaymentUpdateAsync(subscription.UserInternalId, new
            {
                transactionId = subscription.InternalId,
                status = "revoked",
                message = $"Gói Premium của bạn đã bị thu hồi bởi quản trị viên. Lý do: {adminNote ?? "Không có lý do cụ thể"}"
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SignalR Admin Revoke Error]: {ex.Message}");
        }

        return true;
    }

    public async Task<PremiumSubscriptionStatsResponse> GetSubscriptionStatsAsync()
    {
        var now = DateTime.UtcNow;

        // 1. Doanh thu tháng này và biến động
        // Fix timezone: Lấy mốc đầu tháng theo giờ VN (UTC+7) rồi chuyển lại UTC để truy vấn DB
        var localNow = now.AddHours(7);
        var startOfCurrentMonthLocal = new DateTime(localNow.Year, localNow.Month, 1, 0, 0, 0, DateTimeKind.Unspecified);
        var startOfCurrentMonth = DateTime.SpecifyKind(startOfCurrentMonthLocal.AddHours(-7), DateTimeKind.Utc);
        
        var startOfPreviousMonth = startOfCurrentMonth.AddMonths(-1);
        var daysIntoMonth = (now - startOfCurrentMonth).TotalDays;
        var previousMonthMtdEnd = startOfPreviousMonth.AddDays(daysIntoMonth);

        var currentMonthRevenue = await _context.PaymentTransactions
            .Where(t => t.Status == PaymentStatus.Success && t.CreatedAt >= startOfCurrentMonth && t.CreatedAt <= now)
            .SumAsync(t => t.Amount);

        // Sử dụng so sánh Month-to-Date (cùng thời điểm của tháng trước) để không bị lệch số liệu đầu tháng
        var previousMonthRevenue = await _context.PaymentTransactions
            .Where(t => t.Status == PaymentStatus.Success && t.CreatedAt >= startOfPreviousMonth && t.CreatedAt <= previousMonthMtdEnd)
            .SumAsync(t => t.Amount);

        double revenuePercentageChange = previousMonthRevenue == 0 
            ? (currentMonthRevenue > 0 ? 100 : 0) 
            : (double)((currentMonthRevenue - previousMonthRevenue) / previousMonthRevenue * 100);

        // 2. Đăng ký mới 30 ngày và biến động
        var startOfCurrent30Days = now.AddDays(-30);
        var startOfPrevious30Days = now.AddDays(-60);

        var current30DaysSubs = await _context.PremiumSubscriptions
            .Where(ps => ps.CreatedAt >= startOfCurrent30Days && ps.CreatedAt <= now)
            .CountAsync();

        var previous30DaysSubs = await _context.PremiumSubscriptions
            .Where(ps => ps.CreatedAt >= startOfPrevious30Days && ps.CreatedAt < startOfCurrent30Days)
            .CountAsync();

        double newSubsPercentageChange = previous30DaysSubs == 0
            ? (current30DaysSubs > 0 ? 100 : 0)
            : (double)(current30DaysSubs - previous30DaysSubs) / previous30DaysSubs * 100;

        // 3. Tỷ lệ hủy (Churn rate) 30 ngày qua và biến động
        var activeStartCurrent = await _context.PremiumSubscriptions
            .Where(ps => ps.CreatedAt < startOfCurrent30Days && (ps.ExpiresAt == null || ps.ExpiresAt >= startOfCurrent30Days))
            .CountAsync();

        var lostCurrent = await _context.PremiumSubscriptions
            .Where(ps => ps.CreatedAt < startOfCurrent30Days && ps.ExpiresAt >= startOfCurrent30Days && ps.ExpiresAt <= now && ps.IsActive == false)
            .CountAsync();

        double currentChurnRate = activeStartCurrent == 0 ? 0 : (double)lostCurrent / activeStartCurrent * 100;

        var activeStartPrevious = await _context.PremiumSubscriptions
            .Where(ps => ps.CreatedAt < startOfPrevious30Days && (ps.ExpiresAt == null || ps.ExpiresAt >= startOfPrevious30Days))
            .CountAsync();

        var lostPrevious = await _context.PremiumSubscriptions
            .Where(ps => ps.CreatedAt < startOfPrevious30Days && ps.ExpiresAt >= startOfPrevious30Days && ps.ExpiresAt < startOfCurrent30Days && ps.IsActive == false)
            .CountAsync();

        double previousChurnRate = activeStartPrevious == 0 ? 0 : (double)lostPrevious / activeStartPrevious * 100;

        double churnPercentageChange = previousChurnRate == 0
            ? (currentChurnRate > 0 ? 100 : 0)
            : (currentChurnRate - previousChurnRate) / previousChurnRate * 100;

        return new PremiumSubscriptionStatsResponse
        {
            CurrentMonthRevenue = currentMonthRevenue,
            RevenuePercentageChange = Math.Round(revenuePercentageChange, 1),
            NewSubscriptions = current30DaysSubs,
            NewSubscriptionsPercentageChange = Math.Round(newSubsPercentageChange, 1),
            ChurnRate = Math.Round(currentChurnRate, 1),
            ChurnRatePercentageChange = Math.Round(churnPercentageChange, 1)
        };
    }

    public async Task<bool> GrantSubscriptionToUserAsync(GrantSubscriptionRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.InternalId == request.TargetUserId);
        if (user == null || !user.IsActive)
            throw new Exception("Không tìm thấy người dùng nhận gói hoặc tài khoản đã bị khóa.");

        var plan = await _context.SubscriptionPlans.FirstOrDefaultAsync(p => p.Id == request.PlanId);
        if (plan == null || !plan.IsActive)
            throw new Exception("Không tìm thấy gói dịch vụ hoặc gói đã ngưng hoạt động.");

        // Update or Create PremiumSubscription
        var activeSub = await _context.PremiumSubscriptions
            .FirstOrDefaultAsync(s => s.UserInternalId == request.TargetUserId && s.IsActive && (s.ExpiresAt == null || s.ExpiresAt > DateTime.UtcNow));

        if (activeSub != null)
        {
            activeSub.ExpiresAt = activeSub.ExpiresAt.HasValue ? activeSub.ExpiresAt.Value.AddDays((double)(plan.DurationDays ?? 30)) : DateTime.UtcNow.AddDays((double)(plan.DurationDays ?? 30));
            activeSub.SubscriptionPlanInternalId = plan.InternalId;
            activeSub.PlanType = PremiumPlan.Monthly;
            _context.PremiumSubscriptions.Update(activeSub);
        }
        else
        {
            _context.PremiumSubscriptions.Add(new PremiumSubscription
            {
                Id = Guid.NewGuid(),
                UserInternalId = request.TargetUserId,
                SubscriptionPlanInternalId = plan.InternalId,
                PlanType = PremiumPlan.Monthly,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                StartedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays((double)(plan.DurationDays ?? 30)),
                PricePaid = 0,
                Currency = "VND",
                PaymentMethod = "SystemGift"
            });
        }

        // Add Credits
        var customerProfile = await _context.CustomerProfiles.FirstOrDefaultAsync(cp => cp.UserInternalId == request.TargetUserId);
        if (customerProfile != null)
        {
            customerProfile.BgRemovalCredits += plan.GrantedBgCredits;
            customerProfile.TryOnCredits += plan.GrantedTryOnCredits;
            _context.CustomerProfiles.Update(customerProfile);
        }

        // Record transaction
        _context.PaymentTransactions.Add(new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            UserInternalId = request.TargetUserId,
            SubscriptionPlanInternalId = plan.InternalId,
            Amount = 0,
            Currency = "VND",
            PaymentGateway = "SystemGift",
            GatewayTransactionId = "GIFT-" + Guid.NewGuid().ToString("N").Substring(0, 8),
            Status = PaymentStatus.Success,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        // Send notification
        string title = $"Chúc mừng! Bạn đã được tặng gói {plan.Name}";
        string body = string.IsNullOrWhiteSpace(request.AdminNote) 
            ? $"Quản trị viên đã tặng cho bạn gói {plan.Name} với thời hạn {plan.DurationDays} ngày." 
            : $"Bạn nhận được gói {plan.Name} từ Quản trị viên. Lời nhắn: {request.AdminNote}";

        await _notificationService.SendNotificationAsync(
            user.InternalId,
            "System",
            title,
            body,
            "Subscription",
            plan.InternalId,
            sendViaApp: true,
            sendViaEmail: true
        );

        return true;
    }
}
