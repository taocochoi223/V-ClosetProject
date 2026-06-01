using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using VCloset.Application.DTOs.Payment.Responses;
using VCloset.Application.Interfaces;
using VCloset.Domain.Entities;
using VCloset.Infrastructure.Security;

namespace VCloset.Infrastructure.Services;

public class VNPayService : IVNPayService
{
    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public VNPayService(IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
    {
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;
    }

    public Task<VNPayPaymentResponse> CreatePaymentAsync(PaymentTransaction transaction, string planName)
    {
        string tmnCode = Environment.GetEnvironmentVariable("VNPAY_TMN_CODE") ?? _configuration["VNPAY_TMN_CODE"] ?? "";
        string hashSecret = Environment.GetEnvironmentVariable("VNPAY_HASH_SECRET") ?? _configuration["VNPAY_HASH_SECRET"] ?? "";
        string baseUrl = Environment.GetEnvironmentVariable("VNPAY_URL") ?? _configuration["VNPAY_URL"] ?? "";
        string returnUrl = Environment.GetEnvironmentVariable("VNPAY_RETURN_URL") ?? _configuration["VNPAY_RETURN_URL"] ?? "";

        if (string.IsNullOrEmpty(tmnCode) || string.IsNullOrEmpty(hashSecret))
        {
            throw new Exception("Chưa cấu hình VNPay (VNPAY_TMN_CODE hoặc VNPAY_HASH_SECRET bị trống).");
        }

        var vnpay = new VNPayLibrary();

        vnpay.AddRequestData("vnp_Version", "2.1.0");
        vnpay.AddRequestData("vnp_Command", "pay");
        vnpay.AddRequestData("vnp_TmnCode", tmnCode);
        vnpay.AddRequestData("vnp_Amount", ((long)(transaction.Amount * 100)).ToString()); // VNPay requires multiplying by 100
        
        string createDate = DateTime.Now.ToString("yyyyMMddHHmmss");
        vnpay.AddRequestData("vnp_CreateDate", createDate);
        vnpay.AddRequestData("vnp_CurrCode", "VND");
        
        var ipAddress = VNPayLibrary.GetIpAddress(_httpContextAccessor.HttpContext ?? new DefaultHttpContext());
        vnpay.AddRequestData("vnp_IpAddr", ipAddress);
        
        vnpay.AddRequestData("vnp_Locale", "vn");
        vnpay.AddRequestData("vnp_OrderInfo", $"Thanh toan don hang {transaction.InternalId}");
        vnpay.AddRequestData("vnp_OrderType", "other");
        vnpay.AddRequestData("vnp_ReturnUrl", returnUrl);
        vnpay.AddRequestData("vnp_TxnRef", transaction.InternalId.ToString() + "_" + DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()); // Ensure uniqueness

        string paymentUrl = vnpay.CreateRequestUrl(baseUrl, hashSecret);

        return Task.FromResult(new VNPayPaymentResponse { PayUrl = paymentUrl });
    }

    public bool ValidateSignature(string queryString, string hashSecret)
    {
        // For VNPay signature validation, we typically re-parse the query string using the library
        // Since we already receive it as individual query params in the Controller, the Controller will handle it.
        // This is a placeholder since the Controller will use VNPayLibrary directly.
        throw new NotImplementedException();
    }
}
