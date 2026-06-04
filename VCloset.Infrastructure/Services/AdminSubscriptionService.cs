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
            DurationDays = request.DurationDays,
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
        if (request.DurationDays.HasValue) plan.DurationDays = request.DurationDays.Value;
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
}
