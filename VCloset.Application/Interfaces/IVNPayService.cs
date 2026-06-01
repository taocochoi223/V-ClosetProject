using System.Threading.Tasks;
using VCloset.Application.DTOs.Payment.Responses;
using VCloset.Domain.Entities;

namespace VCloset.Application.Interfaces;

public interface IVNPayService
{
    Task<VNPayPaymentResponse> CreatePaymentAsync(PaymentTransaction transaction, string planName);
    bool ValidateSignature(string queryString, string hashSecret);
}
