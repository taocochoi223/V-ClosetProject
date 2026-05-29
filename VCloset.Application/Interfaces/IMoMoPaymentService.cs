using System.Threading.Tasks;
using VCloset.Domain.Entities;

namespace VCloset.Application.Interfaces;

public interface IMoMoPaymentService
{
    Task<string> CreatePaymentAsync(PaymentTransaction transaction, string planName);
    bool ValidateSignature(string rawHash, string signature);
}
