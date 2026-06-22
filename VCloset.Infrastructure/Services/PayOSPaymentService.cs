using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using PayOS;
using PayOS.Models.Webhooks;
using PayOS.Models.V2.PaymentRequests;
using VCloset.Application.DTOs.Payment.Responses;
using VCloset.Application.Interfaces;
using VCloset.Domain.Entities;

namespace VCloset.Infrastructure.Services;

public class PayOSPaymentService : IPayOSPaymentService
{
    private readonly PayOSClient _payOS;
    private readonly string _returnUrl;
    private readonly string _cancelUrl;
    private readonly string _clientId;
    private readonly string _apiKey;
    private readonly string _checksumKey;

    public PayOSPaymentService(IConfiguration configuration)
    {
        _clientId = configuration["PAYOS_CLIENT_ID"] ?? throw new ArgumentNullException("PAYOS_CLIENT_ID");
        _apiKey = configuration["PAYOS_API_KEY"] ?? throw new ArgumentNullException("PAYOS_API_KEY");
        _checksumKey = configuration["PAYOS_CHECKSUM_KEY"] ?? throw new ArgumentNullException("PAYOS_CHECKSUM_KEY");
        
        _payOS = new PayOSClient(_clientId, _apiKey, _checksumKey);
        
        _returnUrl = configuration["VNPAY_RETURN_FE_URL"] ?? "vcloset://payment/result";
        _cancelUrl = configuration["VNPAY_RETURN_FE_URL"] ?? "vcloset://payment/result";
    }

    public async Task<PayOSPaymentResponse> CreatePaymentAsync(VCloset.Domain.Entities.PaymentTransaction transaction, string planName)
    {
        long orderCode = transaction.InternalId;
        if (orderCode == 0) {
            Random random = new Random();
            orderCode = int.Parse(random.Next(100000, 999999).ToString()); 
        }

        var items = new List<PaymentLinkItem> 
        { 
            new PaymentLinkItem { Name = planName, Quantity = 1, Price = (int)transaction.Amount } 
        };
        
        var paymentData = new CreatePaymentLinkRequest
        {
            OrderCode = orderCode,
            Amount = (int)transaction.Amount,
            Description = $"VClo_{transaction.InternalId}",
            Items = items,
            CancelUrl = $"{_cancelUrl}?status=cancelled&gateway=payos",
            ReturnUrl = $"{_returnUrl}?status=success&gateway=payos"
        };

        var createPayment = await _payOS.PaymentRequests.CreateAsync(paymentData);
        
        return new PayOSPaymentResponse
        {
            PayUrl = createPayment.CheckoutUrl,
            QrCodeUrl = createPayment.QrCode
        };
    }

    public WebhookData VerifyWebhook(Webhook webhookBody)
    {
        return _payOS.Webhooks.VerifyAsync(webhookBody).GetAwaiter().GetResult();
    }
}
