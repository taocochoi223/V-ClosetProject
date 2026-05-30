using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VCloset.Application.DTOs.Subscriptions.Responses;
using VCloset.Application.Interfaces;
using VCloset.Domain.Entities;
using VCloset.Domain.Enums;

namespace VCloset.Infrastructure.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMoMoPaymentService _momoPaymentService;

    public SubscriptionService(IUnitOfWork unitOfWork, IMoMoPaymentService momoPaymentService)
    {
        _unitOfWork = unitOfWork;
        _momoPaymentService = momoPaymentService;
    }

    public async Task<IEnumerable<SubscriptionPlanResponse>> GetPlansAsync()
    {
        var plans = await _unitOfWork.SubscriptionPlans.FindAllAsync(p => p.IsActive);
        return plans.OrderBy(p => p.Price).Select(p => new SubscriptionPlanResponse
        {
            Id           = p.Id,
            Name         = p.Name,
            Description  = p.Description,
            Price        = p.Price,
            Currency     = p.Currency,
            DurationDays = p.DurationDays,
            IsActive     = p.IsActive
        });
    }

    public async Task<MySubscriptionResponse> GetMySubscriptionAsync(int userId)
    {
        var now = DateTime.UtcNow;

        var active = await _unitOfWork.PremiumSubscriptions.FindAsync(ps =>
            ps.UserInternalId == userId &&
            ps.IsActive &&
            ps.ExpiresAt > now);

        var profile = await _unitOfWork.CustomerProfiles.FindAsync(c => c.UserInternalId == userId);

        var response = new MySubscriptionResponse
        {
            HasActivePremium  = active != null,
            WardrobeItemCount = profile?.WardrobeItemCount ?? 0,
            BgRemovalCredits  = 0,
            TryOnCredits      = 0,
        };

        if (active != null)
        {
            SubscriptionPlan? plan = null;
            if (active.SubscriptionPlanInternalId.HasValue)
                plan = await _unitOfWork.SubscriptionPlans.GetByIdAsync(active.SubscriptionPlanInternalId.Value);

            response.PlanName         = plan?.Name ?? active.PlanType.ToString();
            response.PlanType         = active.PlanType.ToString().ToLower();
            response.ExpiresAt        = active.ExpiresAt;
            response.DaysRemaining    = (int)Math.Max(0, Math.Ceiling((active.ExpiresAt - now).TotalDays));
            response.WardrobeItemLimit = null; // Premium = không giới hạn
        }
        else
        {
            response.PlanName          = "Miễn phí";
            response.PlanType          = "free";
            response.WardrobeItemLimit = 30;
        }

        return response;
    }

    public async Task<IEnumerable<PaymentTransactionResponse>> GetMyTransactionsAsync(int userId)
    {
        var transactions = await _unitOfWork.PaymentTransactions.FindAllAsync(
            t => t.UserInternalId == userId);

        var result = new List<PaymentTransactionResponse>();
        foreach (var t in transactions.OrderByDescending(t => t.CreatedAt))
        {
            var plan = await _unitOfWork.SubscriptionPlans.GetByIdAsync(t.SubscriptionPlanInternalId);
            result.Add(new PaymentTransactionResponse
            {
                Id                   = t.Id,
                PlanName             = plan?.Name ?? "Không xác định",
                Amount               = t.Amount,
                Currency             = t.Currency,
                PaymentGateway       = t.PaymentGateway,
                Status               = t.Status.ToString().ToLower(),
                GatewayTransactionId = t.GatewayTransactionId,
                CreatedAt            = t.CreatedAt
            });
        }
        return result;
    }

    public async Task<VCloset.Application.DTOs.Payment.Responses.MoMoPaymentResponse> InitiatePurchaseAsync(int userId, Guid planId)
    {
        var plan = await _unitOfWork.SubscriptionPlans.FindAsync(p => p.Id == planId && p.IsActive);
        if (plan == null)
            throw new Exception("Gói dịch vụ không tồn tại hoặc đã ngừng cung cấp.");

        var transaction = new PaymentTransaction
        {
            Id                          = Guid.NewGuid(),
            UserInternalId              = userId,
            SubscriptionPlanInternalId  = plan.InternalId,
            Amount                      = plan.Price,
            Currency                    = plan.Currency,
            PaymentGateway              = "momo",
            Status                      = PaymentStatus.Pending,
            CreatedAt                   = DateTime.UtcNow,
            UpdatedAt                   = DateTime.UtcNow
        };

        await _unitOfWork.PaymentTransactions.AddAsync(transaction);
        await _unitOfWork.SaveChangesAsync();

        var paymentResponse = await _momoPaymentService.CreatePaymentAsync(transaction, plan.Name);

        return paymentResponse;
    }
}
