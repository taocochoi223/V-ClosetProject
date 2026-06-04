using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VCloset.Application.DTOs.Subscriptions.Requests;
using VCloset.Application.DTOs.Subscriptions.Responses;
using VCloset.Application.Interfaces;
using VCloset.Domain.Entities;
using VCloset.Infrastructure.Data;

namespace VCloset.Infrastructure.Services;

public class AdminSubscriptionService : IAdminSubscriptionService
{
    private readonly VClosetVersion30Context _context;

    public AdminSubscriptionService(VClosetVersion30Context context)
    {
        _context = context;
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
            DurationDays = request.DurationDays == 0 ? null : request.DurationDays,
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
        if (request.DurationDays.HasValue) plan.DurationDays = request.DurationDays.Value == 0 ? null : request.DurationDays.Value;
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
}
