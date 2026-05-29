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
    private readonly IMoMoPaymentService _momoPaymentService;

    public PaymentsController(IUnitOfWork unitOfWork, IMoMoPaymentService momoPaymentService)
    {
        _unitOfWork = unitOfWork;
        _momoPaymentService = momoPaymentService;
    }

    /// <summary>
    /// IPN Webhook dành cho MoMo gọi ngầm báo kết quả thanh toán
    /// </summary>
    [HttpPost("momo/ipn")]
    [AllowAnonymous]
    public async Task<IActionResult> MoMoWebhook([FromBody] JsonElement requestBody)
    {
        try
        {
            string partnerCode = requestBody.GetProperty("partnerCode").GetString() ?? "";
            string orderId = requestBody.GetProperty("orderId").GetString() ?? "";
            string requestId = requestBody.GetProperty("requestId").GetString() ?? "";
            string amount = requestBody.GetProperty("amount").GetRawText() ?? "";
            string orderInfo = requestBody.GetProperty("orderInfo").GetString() ?? "";
            string requestType = requestBody.GetProperty("requestType").GetString() ?? "";
            string transId = requestBody.GetProperty("transId").GetRawText() ?? "";
            string resultCode = requestBody.GetProperty("resultCode").GetRawText() ?? "";
            string message = requestBody.GetProperty("message").GetString() ?? "";
            string payType = requestBody.GetProperty("payType").GetString() ?? "";
            string responseTime = requestBody.GetProperty("responseTime").GetRawText() ?? "";
            string extraData = requestBody.GetProperty("extraData").GetString() ?? "";
            string signature = requestBody.GetProperty("signature").GetString() ?? "";

            string accessKey = Environment.GetEnvironmentVariable("MOMO_ACCESS_KEY") ?? "";
            
            string rawHash = $"accessKey={accessKey}&amount={amount}&extraData={extraData}&message={message}&orderId={orderId}&orderInfo={orderInfo}&partnerCode={partnerCode}&payType={payType}&requestId={requestId}&responseTime={responseTime}&resultCode={resultCode}";
            
            if (!_momoPaymentService.ValidateSignature(rawHash, signature))
            {
                return BadRequest(new { message = "Invalid signature" });
            }

            if (int.TryParse(extraData, out int transactionInternalId))
            {
                var transaction = await _unitOfWork.PaymentTransactions.GetByIdAsync(transactionInternalId);
                if (transaction != null && transaction.Status == PaymentStatus.Pending)
                {
                    transaction.GatewayTransactionId = transId;
                    transaction.RawCallbackData = requestBody.GetRawText();
                    transaction.UpdatedAt = DateTime.UtcNow;

                    if (resultCode == "0") // 0 = Thành công
                    {
                        transaction.Status = PaymentStatus.Success;

                        var plan = await _unitOfWork.SubscriptionPlans.GetByIdAsync(transaction.SubscriptionPlanInternalId);
                        if (plan != null)
                        {
                            var existingPremium = await _unitOfWork.PremiumSubscriptions.FindAsync(
                                ps => ps.UserInternalId == transaction.UserInternalId && ps.IsActive);

                            if (existingPremium != null)
                            {
                                existingPremium.ExpiresAt = existingPremium.ExpiresAt > DateTime.UtcNow 
                                    ? existingPremium.ExpiresAt.AddDays(plan.DurationDays) 
                                    : DateTime.UtcNow.AddDays(plan.DurationDays);
                            }
                            else
                            {
                                var newPremium = new PremiumSubscription
                                {
                                    Id = Guid.NewGuid(),
                                    UserInternalId = transaction.UserInternalId,
                                    SubscriptionPlanInternalId = plan.InternalId,
                                    PlanType = (PremiumPlan)plan.InternalId, // Assuming ID maps to Enum
                                    PricePaid = transaction.Amount,
                                    Currency = transaction.Currency,
                                    PaymentMethod = "momo",
                                    PaymentRef = transId,
                                    StartedAt = DateTime.UtcNow,
                                    ExpiresAt = DateTime.UtcNow.AddDays(plan.DurationDays),
                                    IsActive = true,
                                    CreatedAt = DateTime.UtcNow
                                };
                                await _unitOfWork.PremiumSubscriptions.AddAsync(newPremium);
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

            return NoContent();
        }
        catch (Exception ex)
        {
            Console.WriteLine("MoMo IPN Error: " + ex.Message);
            return BadRequest();
        }
    }
}
