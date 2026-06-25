using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VCloset.Application.DTOs.Subscriptions.Responses;
using VCloset.Application.Interfaces;
using VCloset.Domain.Entities;
using VCloset.Domain.Enums;

namespace VCloset.Infrastructure.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPayOSPaymentService _payOSPaymentService;
    private readonly ITierConfigService _tierConfigService;

    public SubscriptionService(IUnitOfWork unitOfWork, IPayOSPaymentService payOSPaymentService, ITierConfigService tierConfigService)
    {
        _unitOfWork = unitOfWork;
        _payOSPaymentService = payOSPaymentService;
        _tierConfigService = tierConfigService;
    }

    public async Task<IEnumerable<SubscriptionPlanResponse>> GetPlansAsync()
    {
        var plans = await _unitOfWork.SubscriptionPlans.FindAllAsync(p => p.IsActive);
        var planList = plans.ToList();
        bool needsSave = false;

        if (!planList.Any(p => p.Name.Contains("10 Credits") || p.Id == Guid.Parse("fa719b0a-3135-4309-847e-855f7bc74e6c")))
        {
            var p3 = new SubscriptionPlan
            {
                Id = Guid.Parse("fa719b0a-3135-4309-847e-855f7bc74e6c"),
                Name = "Gói 10 Credits Thử đồ AI",
                Description = "Cộng thêm 10 lượt thử đồ AI ảo, sử dụng bất cứ lúc nào.",
                Price = 29000m,
                Currency = "VND",
                DurationDays = null,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _unitOfWork.SubscriptionPlans.AddAsync(p3);
            planList.Add(p3);
            needsSave = true;
        }

        if (!planList.Any(p => p.Name.Contains("25 Credits") || p.Id == Guid.Parse("96a84d28-3e5f-4a0b-9df0-dfd35a8bc589")))
        {
            var p4 = new SubscriptionPlan
            {
                Id = Guid.Parse("96a84d28-3e5f-4a0b-9df0-dfd35a8bc589"),
                Name = "Gói 25 Credits Thử đồ AI",
                Description = "Cộng thêm 25 lượt thử đồ AI ảo, sử dụng bất cứ lúc nào.",
                Price = 69000m,
                Currency = "VND",
                DurationDays = null,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _unitOfWork.SubscriptionPlans.AddAsync(p4);
            planList.Add(p4);
            needsSave = true;
        }

        if (needsSave)
        {
            await _unitOfWork.SaveChangesAsync();
        }

        return planList.OrderBy(p => p.Price).Select(p => new SubscriptionPlanResponse
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
            (!ps.ExpiresAt.HasValue || ps.ExpiresAt > now));

        var profile = await _unitOfWork.CustomerProfiles.FindAsync(c => c.UserInternalId == userId);

        // Lazy Update Check: Check if there's any active subscription that has just expired
        if (active == null)
        {
            var expiredActiveSub = await _unitOfWork.PremiumSubscriptions.FindAsync(ps =>
                ps.UserInternalId == userId &&
                ps.IsActive &&
                ps.ExpiresAt.HasValue &&
                ps.ExpiresAt.Value <= now);

            if (expiredActiveSub != null)
            {
                // 1. Deactivate subscription
                expiredActiveSub.IsActive = false;
                expiredActiveSub.CancelledAt = now;
                _unitOfWork.PremiumSubscriptions.Update(expiredActiveSub);

                // 2. Reset profile credits to free tier limits
                if (profile != null)
                {
                    var freeTier = await _tierConfigService.GetConfigEntityAsync("free");
                    if (freeTier != null)
                    {
                        profile.BgRemovalCredits = freeTier.BgRemovalCredits;
                        profile.TryOnCredits = freeTier.TryOnCredits;
                        profile.UpdatedAt = now;
                        _unitOfWork.CustomerProfiles.Update(profile);
                    }
                }

                await _unitOfWork.SaveChangesAsync();
            }
        }

        var outfitCount = await _unitOfWork.CanvasOutfits.Query()
            .CountAsync(o => o.UserInternalId == userId);

        var surveyUrlSetting = await _unitOfWork.SystemSettings.FindAsync(s => s.SettingKey == "survey_url");
        var surveyUrl = surveyUrlSetting?.SettingValue ?? "https://forms.gle/YOUR_GOOGLE_FORM_LINK";

        var response = new MySubscriptionResponse
        {
            HasActivePremium  = active != null,
            WardrobeItemCount = profile?.WardrobeItemCount ?? 0,
            BgRemovalCredits  = profile?.BgRemovalCredits ?? 1,
            TryOnCredits      = profile?.TryOnCredits ?? 1,
            OutfitCount       = outfitCount,
            HasCompletedSurvey = profile?.HasCompletedSurvey ?? false,
            SurveyUrl         = surveyUrl,
        };

        if (active != null)
        {
            SubscriptionPlan? plan = null;
            if (active.SubscriptionPlanInternalId.HasValue)
                plan = await _unitOfWork.SubscriptionPlans.GetByIdAsync(active.SubscriptionPlanInternalId.Value);

            response.PlanName         = plan?.Name ?? active.PlanType.ToString();
            response.PlanType         = active.PlanType.ToString().ToLower();
            response.ExpiresAt        = active.ExpiresAt;
            response.DaysRemaining    = active.ExpiresAt.HasValue ? (int)Math.Max(0, Math.Ceiling((active.ExpiresAt.Value - now).TotalDays)) : 0;
            var premiumTier = await _tierConfigService.GetConfigEntityAsync("premium");
            response.WardrobeItemLimit = premiumTier.WardrobeItemLimit;
            response.OutfitLimit       = premiumTier.OutfitLimit;
        }
        else
        {
            response.PlanName          = "Miễn phí";
            response.PlanType          = "free";
            var freeTier = await _tierConfigService.GetConfigEntityAsync("free");
            response.WardrobeItemLimit = freeTier.WardrobeItemLimit;
            response.OutfitLimit       = freeTier.OutfitLimit;
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

    public async Task<VCloset.Application.DTOs.Payment.Responses.PaymentInitializationResponse> InitiatePurchaseAsync(int userId, Guid planId, string? couponCode = null, string paymentGateway = "payos")
    {
        paymentGateway = paymentGateway.ToLower();
        if (paymentGateway != "payos")
        {
            throw new Exception("Cổng thanh toán không hợp lệ (hiện tại chỉ hỗ trợ payos).");
        }

        var plan = await _unitOfWork.SubscriptionPlans.FindAsync(p => p.Id == planId && p.IsActive);
        if (plan == null)
            throw new Exception("Gói dịch vụ không tồn tại hoặc đã ngừng cung cấp.");

        decimal finalPrice = plan.Price;
        string? appliedCoupon = null;

        if (!string.IsNullOrWhiteSpace(couponCode))
        {
            var coupon = await _unitOfWork.Coupons.FindAsync(c => c.Code.ToLower() == couponCode.ToLower());
            if (coupon == null || !coupon.IsActive)
                throw new Exception("Mã giảm giá không hợp lệ.");

            if (coupon.ExpiresAt.HasValue && coupon.ExpiresAt.Value < DateTime.UtcNow)
                throw new Exception("Mã giảm giá đã hết hạn.");

            if (coupon.MaxUses.HasValue && coupon.CurrentUses >= coupon.MaxUses.Value)
                throw new Exception("Mã giảm giá đã hết lượt sử dụng.");

            if (coupon.DiscountType == DiscountType.Percentage)
            {
                decimal discount = plan.Price * (coupon.DiscountValue / 100m);
                finalPrice = plan.Price - discount;
            }
            else if (coupon.DiscountType == DiscountType.FixedAmount)
            {
                finalPrice = plan.Price - coupon.DiscountValue;
            }

            if (finalPrice < 0) finalPrice = 0;
            appliedCoupon = coupon.Code.ToUpper();
        }

        var transaction = new PaymentTransaction
        {
            Id                          = Guid.NewGuid(),
            UserInternalId              = userId,
            SubscriptionPlanInternalId  = plan.InternalId,
            Amount                      = finalPrice,
            Currency                    = plan.Currency,
            PaymentGateway              = paymentGateway,
            Status                      = PaymentStatus.Pending,
            AppliedCouponCode           = appliedCoupon,
            CreatedAt                   = DateTime.UtcNow,
            UpdatedAt                   = DateTime.UtcNow
        };

        await _unitOfWork.PaymentTransactions.AddAsync(transaction);
        await _unitOfWork.SaveChangesAsync();

        var payosResponse = await _payOSPaymentService.CreatePaymentAsync(transaction, plan.Name);
        return new VCloset.Application.DTOs.Payment.Responses.PaymentInitializationResponse
        {
            PayUrl = payosResponse.PayUrl,
            QrCodeUrl = payosResponse.QrCodeUrl,
            PaymentGateway = "payos"
        };
    }

    public async Task<MySubscriptionResponse> ClaimAdRewardAsync(int userId, string rewardType)
    {
        var profile = await _unitOfWork.CustomerProfiles.FindAsync(c => c.UserInternalId == userId);
        if (profile == null)
            throw new Exception("Không tìm thấy thông tin cá nhân khách hàng.");

        rewardType = rewardType.ToLower().Trim();
        if (rewardType == "bg_removal" || rewardType == "bgremoval")
        {
            profile.BgRemovalCredits += 1;
        }
        else if (rewardType == "try_on" || rewardType == "tryon")
        {
            // Tắt tính năng đổi quảng cáo lấy lượt thử đồ AI do chi phí GPU cao
            throw new InvalidOperationException("Không hỗ trợ nhận lượt thử đồ AI miễn phí qua quảng cáo. Vui lòng nâng cấp gói Premium.");
        }
        else if (rewardType == "survey")
        {
            if (profile.HasCompletedSurvey)
            {
                throw new InvalidOperationException("Bạn đã nhận 3 lượt thử đồ miễn phí từ khảo sát này rồi. Mỗi tài khoản chỉ được nhận một lần.");
            }
            profile.TryOnCredits += 3;
            profile.HasCompletedSurvey = true;
        }
        else
        {
            throw new ArgumentException("Loại phần thưởng không hợp lệ.");
        }

        await _unitOfWork.SaveChangesAsync();

        return await GetMySubscriptionAsync(userId);
    }
}
