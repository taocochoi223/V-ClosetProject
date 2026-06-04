using System.Threading.Tasks;

namespace VCloset.Application.Interfaces;

public interface IEmailService
{
    Task<bool> SendEmailAsync(string toEmail, string subject, string htmlContent);
    Task<bool> SendOtpEmailAsync(string toEmail, string otpCode);
    Task<bool> SendForgotPasswordOtpEmailAsync(string toEmail, string otpCode);
    Task<bool> SendNewAccountEmailAsync(string toEmail, string displayName, string tempPassword);
    Task<bool> SendAdminPaymentNotificationAsync(string toEmail, string senderName, string senderEmail, string planName, decimal amount, string currency, string userNote, DateTime createdAt);
}
