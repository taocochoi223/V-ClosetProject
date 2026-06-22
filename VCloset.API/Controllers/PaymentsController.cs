using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VCloset.Application.Interfaces;
using VCloset.Domain.Entities;
using VCloset.Domain.Enums;

namespace VCloset.API.Controllers;

[Route("api/payments")]
[ApiController]
public class PaymentsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPayOSPaymentService _payOSPaymentService;
    private readonly ITierConfigService _tierConfigService;

    public PaymentsController(IUnitOfWork unitOfWork, IPayOSPaymentService payOSPaymentService, ITierConfigService tierConfigService)
    {
        _unitOfWork = unitOfWork;
        _payOSPaymentService = payOSPaymentService;
        _tierConfigService = tierConfigService;
    }



    /// <summary>
    /// Webhook dành cho PayOS gọi ngầm báo kết quả thanh toán
    /// </summary>
    [HttpPost("payos/webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> PayOSWebhook([FromBody] PayOS.Models.Webhooks.Webhook webhookBody)
    {
        try
        {
            var webhookData = _payOSPaymentService.VerifyWebhook(webhookBody);

            int transactionInternalId = (int)webhookData.OrderCode;

            if (transactionInternalId > 0)
            {
                var transaction = await _unitOfWork.PaymentTransactions.GetByIdAsync(transactionInternalId);
                if (transaction != null && transaction.Status == PaymentStatus.Pending)
                {
                    transaction.GatewayTransactionId = webhookData.Reference;
                    transaction.RawCallbackData = JsonSerializer.Serialize(webhookBody);
                    transaction.UpdatedAt = DateTime.UtcNow;

                    if (webhookData.Code == "00") // 00 = Thành công
                    {
                        transaction.Status = PaymentStatus.Success;

                        var plan = await _unitOfWork.SubscriptionPlans.GetByIdAsync(transaction.SubscriptionPlanInternalId);
                        if (plan != null)
                        {
                            var profile = await _unitOfWork.CustomerProfiles.FindAsync(cp => cp.UserInternalId == transaction.UserInternalId);
                            if (profile != null)
                            {
                                profile.BgRemovalCredits += plan.GrantedBgCredits;
                                profile.TryOnCredits += plan.GrantedTryOnCredits;
                                profile.UpdatedAt = DateTime.UtcNow;
                                _unitOfWork.CustomerProfiles.Update(profile);
                            }

                            if (plan.DurationDays.HasValue && plan.DurationDays.Value > 0)
                            {
                                var existingPremium = await _unitOfWork.PremiumSubscriptions.FindAsync(
                                    ps => ps.UserInternalId == transaction.UserInternalId && ps.IsActive);

                                if (existingPremium != null)
                                {
                                    if (existingPremium.ExpiresAt.HasValue)
                                    {
                                        existingPremium.ExpiresAt = existingPremium.ExpiresAt.Value > DateTime.UtcNow 
                                            ? existingPremium.ExpiresAt.Value.AddDays(plan.DurationDays.Value) 
                                            : DateTime.UtcNow.AddDays(plan.DurationDays.Value);
                                    }
                                }
                                else
                                {
                                    var newPremium = new PremiumSubscription
                                    {
                                        Id = Guid.NewGuid(),
                                        UserInternalId = transaction.UserInternalId,
                                        SubscriptionPlanInternalId = plan.InternalId,
                                        PlanType = plan.DurationDays >= 365 ? PremiumPlan.Yearly : PremiumPlan.Monthly,
                                        PricePaid = transaction.Amount,
                                        Currency = transaction.Currency,
                                        PaymentMethod = "payos",
                                        PaymentRef = webhookData.Reference,
                                        StartedAt = DateTime.UtcNow,
                                        ExpiresAt = DateTime.UtcNow.AddDays(plan.DurationDays.Value),
                                        IsActive = true,
                                        CreatedAt = DateTime.UtcNow
                                    };
                                    await _unitOfWork.PremiumSubscriptions.AddAsync(newPremium);
                                }
                            }
                        }
                    }
                    else
                    {
                        transaction.Status = PaymentStatus.Failed;
                    }

                    await _unitOfWork.SaveChangesAsync();
                }
            }

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            Console.WriteLine("PayOS Webhook Error: " + ex.Message);
            return Ok(new { success = false, message = "Webhook processing error" });
        }
    }
}
