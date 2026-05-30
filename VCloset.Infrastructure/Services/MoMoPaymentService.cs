using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using VCloset.Application.DTOs.Payment.Responses;
using VCloset.Application.Interfaces;
using VCloset.Domain.Entities;

namespace VCloset.Infrastructure.Services;

public class MoMoPaymentService : IMoMoPaymentService
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;

    public MoMoPaymentService(IConfiguration configuration, HttpClient httpClient)
    {
        _configuration = configuration;
        _httpClient = httpClient;
    }

    public async Task<MoMoPaymentResponse> CreatePaymentAsync(PaymentTransaction transaction, string planName)
    {
        string partnerCode = Environment.GetEnvironmentVariable("MOMO_PARTNER_CODE") ?? _configuration["MOMO_PARTNER_CODE"] ?? "";
        string accessKey = Environment.GetEnvironmentVariable("MOMO_ACCESS_KEY") ?? _configuration["MOMO_ACCESS_KEY"] ?? "";
        string secretKey = Environment.GetEnvironmentVariable("MOMO_SECRET_KEY") ?? _configuration["MOMO_SECRET_KEY"] ?? "";
        string endpoint = Environment.GetEnvironmentVariable("MOMO_ENDPOINT") ?? _configuration["MOMO_ENDPOINT"] ?? "";
        string returnUrl = Environment.GetEnvironmentVariable("MOMO_RETURN_URL") ?? _configuration["MOMO_RETURN_URL"] ?? "";
        string notifyUrl = Environment.GetEnvironmentVariable("MOMO_NOTIFY_URL") ?? _configuration["MOMO_NOTIFY_URL"] ?? "";

        if (string.IsNullOrEmpty(partnerCode) || string.IsNullOrEmpty(secretKey))
        {
            throw new Exception("Chưa cấu hình MoMo (MOMO_PARTNER_CODE hoặc MOMO_SECRET_KEY bị trống).");
        }

        string orderId = transaction.InternalId.ToString() + "_" + DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string requestId = Guid.NewGuid().ToString();
        long amountValue = (long)transaction.Amount;
        string amountStr = amountValue.ToString();
        string orderInfo = $"Thanh toan V-Closet: {planName}";
        string requestType = "captureWallet";
        string extraData = ""; // Docs require Base64 JSON or empty string

        string rawHash = $"accessKey={accessKey}&amount={amountStr}&extraData={extraData}&ipnUrl={notifyUrl}&orderId={orderId}&orderInfo={orderInfo}&partnerCode={partnerCode}&redirectUrl={returnUrl}&requestId={requestId}&requestType={requestType}";
        string signature = ComputeHmacSha256(rawHash, secretKey);

        var requestData = new
        {
            partnerCode,
            partnerName = "V-Closet",
            storeId = "V-Closet-Store",
            requestId,
            amount = amountValue, // Must be long in JSON
            orderId,
            orderInfo,
            redirectUrl = returnUrl,
            ipnUrl = notifyUrl,
            lang = "vi",
            extraData,
            requestType,
            signature
        };

        var content = new StringContent(JsonSerializer.Serialize(requestData), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(endpoint, content);
        
        var responseString = await response.Content.ReadAsStringAsync();
        
        using var doc = JsonDocument.Parse(responseString);
        var responseDto = new MoMoPaymentResponse();

        if (doc.RootElement.TryGetProperty("payUrl", out var payUrlElement))
        {
            responseDto.PayUrl = payUrlElement.GetString() ?? "";
        }
        if (doc.RootElement.TryGetProperty("deeplink", out var deeplinkElement))
        {
            responseDto.Deeplink = deeplinkElement.GetString() ?? "";
        }
        if (doc.RootElement.TryGetProperty("qrCodeUrl", out var qrCodeUrlElement))
        {
            responseDto.QrCodeUrl = qrCodeUrlElement.GetString() ?? "";
        }

        if (!string.IsNullOrEmpty(responseDto.PayUrl))
        {
            return responseDto;
        }

        throw new Exception("Lỗi tạo thanh toán MoMo: " + responseString);
    }

    public bool ValidateSignature(string rawHash, string signature)
    {
        string secretKey = Environment.GetEnvironmentVariable("MOMO_SECRET_KEY") ?? _configuration["MOMO_SECRET_KEY"] ?? "";
        string expectedSignature = ComputeHmacSha256(rawHash, secretKey);
        return signature == expectedSignature;
    }

    private string ComputeHmacSha256(string message, string secretKey)
    {
        if (string.IsNullOrEmpty(secretKey)) return "";
        
        var keyBytes = Encoding.UTF8.GetBytes(secretKey);
        var messageBytes = Encoding.UTF8.GetBytes(message);

        using (var hmacsha256 = new HMACSHA256(keyBytes))
        {
            var hashmessage = hmacsha256.ComputeHash(messageBytes);
            return BitConverter.ToString(hashmessage).Replace("-", "").ToLower();
        }
    }
}
