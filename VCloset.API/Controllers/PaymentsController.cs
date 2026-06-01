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
    private readonly IVNPayService _vnPayService;

    public PaymentsController(IUnitOfWork unitOfWork, IMoMoPaymentService momoPaymentService, IVNPayService vnPayService)
    {
        _unitOfWork = unitOfWork;
        _momoPaymentService = momoPaymentService;
        _vnPayService = vnPayService;
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
            string orderType = requestBody.TryGetProperty("orderType", out var ot) ? ot.GetString() ?? "" : "momo_wallet";
            string transId = requestBody.GetProperty("transId").GetRawText() ?? "";
            string resultCode = requestBody.GetProperty("resultCode").GetRawText() ?? "";
            string message = requestBody.GetProperty("message").GetString() ?? "";
            string payType = requestBody.GetProperty("payType").GetString() ?? "";
            string responseTime = requestBody.GetProperty("responseTime").GetRawText() ?? "";
            string extraData = requestBody.GetProperty("extraData").GetString() ?? "";
            string signature = requestBody.GetProperty("signature").GetString() ?? "";

            string accessKey = Environment.GetEnvironmentVariable("MOMO_ACCESS_KEY") ?? "";
            
            string rawHash = $"accessKey={accessKey}&amount={amount}&extraData={extraData}&message={message}&orderId={orderId}&orderInfo={orderInfo}&orderType={orderType}&partnerCode={partnerCode}&payType={payType}&requestId={requestId}&responseTime={responseTime}&resultCode={resultCode}&transId={transId}";
            
            if (!_momoPaymentService.ValidateSignature(rawHash, signature))
            {
                return BadRequest(new { message = "Invalid signature" });
            }

            int transactionInternalId = 0;
            var orderIdParts = orderId.Split('_');
            if (orderIdParts.Length > 0)
            {
                int.TryParse(orderIdParts[0], out transactionInternalId);
            }

            if (transactionInternalId > 0)
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
                                    PlanType = plan.DurationDays >= 365 ? PremiumPlan.Yearly : PremiumPlan.Monthly,
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

    /// <summary>
    /// IPN Webhook dành cho VNPay gọi ngầm báo kết quả thanh toán
    /// </summary>
    [HttpGet("vnpay/ipn")]
    [AllowAnonymous]
    public async Task<IActionResult> VNPayIpnWebhook()
    {
        try
        {
            var queryDictionary = Request.Query.ToDictionary(q => q.Key, q => q.Value.ToString());
            
            string hashSecret = Environment.GetEnvironmentVariable("VNPAY_HASH_SECRET") ?? "";
            
            var vnpay = new VCloset.Infrastructure.Security.VNPayLibrary();
            foreach (var (key, value) in queryDictionary)
            {
                if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_"))
                {
                    vnpay.AddResponseData(key, value);
                }
            }

            string vnp_SecureHash = queryDictionary.TryGetValue("vnp_SecureHash", out var hash) ? hash : "";
            bool isValid = vnpay.ValidateSignature(vnp_SecureHash, hashSecret);

            if (!isValid)
            {
                return Ok(new { RspCode = "97", Message = "Invalid signature" });
            }

            string vnp_TxnRef = vnpay.GetResponseData("vnp_TxnRef");
            string vnp_ResponseCode = vnpay.GetResponseData("vnp_ResponseCode");
            string vnp_TransactionNo = vnpay.GetResponseData("vnp_TransactionNo");

            int transactionInternalId = 0;
            var orderIdParts = vnp_TxnRef.Split('_');
            if (orderIdParts.Length > 0)
            {
                int.TryParse(orderIdParts[0], out transactionInternalId);
            }

            if (transactionInternalId > 0)
            {
                var transaction = await _unitOfWork.PaymentTransactions.GetByIdAsync(transactionInternalId);
                if (transaction != null)
                {
                    if (transaction.Status != PaymentStatus.Pending)
                    {
                        return Ok(new { RspCode = "02", Message = "Order already confirmed" });
                    }

                    transaction.GatewayTransactionId = vnp_TransactionNo;
                    transaction.RawCallbackData = JsonSerializer.Serialize(queryDictionary);
                    transaction.UpdatedAt = DateTime.UtcNow;

                    if (vnp_ResponseCode == "00") // 00 = Thành công
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
                                    PlanType = plan.DurationDays >= 365 ? PremiumPlan.Yearly : PremiumPlan.Monthly,
                                    PricePaid = transaction.Amount,
                                    Currency = transaction.Currency,
                                    PaymentMethod = "vnpay",
                                    PaymentRef = vnp_TransactionNo,
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
                    return Ok(new { RspCode = "00", Message = "Confirm Success" });
                }
                return Ok(new { RspCode = "01", Message = "Order not found" });
            }
            return Ok(new { RspCode = "01", Message = "Order not found" });
        }
        catch (Exception ex)
        {
            Console.WriteLine("VNPay IPN Error: " + ex.Message);
            return Ok(new { RspCode = "99", Message = "Unknown error" });
        }
    }

    /// <summary>
    /// Return Webhook dành cho user sau khi thanh toán VNPay xong
    /// </summary>
    [HttpGet("vnpay/return")]
    [AllowAnonymous]
    public IActionResult VNPayReturn()
    {
        // VNPay Return URL is just to display result to user, the actual DB update is handled by IPN.
        // Usually we redirect to FE.
        var queryDictionary = Request.Query.ToDictionary(q => q.Key, q => q.Value.ToString());
        string vnp_ResponseCode = queryDictionary.TryGetValue("vnp_ResponseCode", out var code) ? code : "";
        string returnUrl = Environment.GetEnvironmentVariable("VNPAY_RETURN_FE_URL") ?? "vcloset://payment/result";

        string redirectUrl = $"{returnUrl}?resultCode={(vnp_ResponseCode == "00" ? "success" : "failed")}";
        return Redirect(redirectUrl);
    }
}
