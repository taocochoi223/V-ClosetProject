using System.Threading.Tasks;
using VCloset.Application.DTOs.Payment.Responses;
using VCloset.Domain.Entities;

namespace VCloset.Application.Interfaces;

public interface IMoMoPaymentService
{
    Task<MoMoPaymentResponse> CreatePaymentAsync(PaymentTransaction transaction, string planName);
    bool ValidateSignature(string rawHash, string signature);
}
