using System.Threading.Tasks;
using PayOS.Models.Webhooks;
using VCloset.Application.DTOs.Payment.Responses;
using VCloset.Domain.Entities;

namespace VCloset.Application.Interfaces;

public interface IPayOSPaymentService
{
    Task<PayOSPaymentResponse> CreatePaymentAsync(VCloset.Domain.Entities.PaymentTransaction transaction, string planName);
    WebhookData VerifyWebhook(Webhook webhookBody);
}
