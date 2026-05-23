using System.Threading.Tasks;

namespace VCloset.Application.Interfaces;

public interface IEmailService
{
    Task<bool> SendEmailAsync(string toEmail, string subject, string htmlContent);
    Task<bool> SendOtpEmailAsync(string toEmail, string otpCode);
    Task<bool> SendForgotPasswordOtpEmailAsync(string toEmail, string otpCode);
}
