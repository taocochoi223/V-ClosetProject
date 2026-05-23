using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using VCloset.Application.Interfaces;

namespace VCloset.Infrastructure.Services;

public class SmtpEmailService : IEmailService
{
    private readonly string _smtpHost = string.Empty;
    private readonly int _smtpPort;
    private readonly string _fromEmail = string.Empty;
    private readonly string _smtpPassword = string.Empty;

    public SmtpEmailService()
    {
        _smtpHost = Environment.GetEnvironmentVariable("SMTP_HOST") ?? "smtp.gmail.com";
        _smtpPort = int.TryParse(Environment.GetEnvironmentVariable("SMTP_PORT"), out var port) ? port : 587;
        _fromEmail = Environment.GetEnvironmentVariable("SMTP_EMAIL") ?? string.Empty;
        _smtpPassword = Environment.GetEnvironmentVariable("SMTP_PASSWORD") ?? string.Empty;

        // Log chẩn đoán cấu hình khi Service khởi tạo
        Console.WriteLine($"[SMTP CONFIG] Host: {_smtpHost}, Port: {_smtpPort}, FromEmail: '{_fromEmail}', Password Length: {_smtpPassword?.Length ?? 0}");
    }

    public async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlContent)
    {
        if (string.IsNullOrEmpty(_fromEmail) || string.IsNullOrEmpty(_smtpPassword))
        {
            Console.WriteLine($"[SMTP ERROR] Credentials are missing. FromEmail is '{_fromEmail}', Password is missing? {string.IsNullOrEmpty(_smtpPassword)}");
            return false;
        }

        try
        {
            using var mailMessage = new MailMessage();
            mailMessage.From = new MailAddress(_fromEmail, "V-Closet Support");
            mailMessage.To.Add(toEmail);
            mailMessage.Subject = subject;
            mailMessage.Body = htmlContent;
            mailMessage.IsBodyHtml = true;

            using var smtpClient = new SmtpClient(_smtpHost, _smtpPort);
            smtpClient.Credentials = new NetworkCredential(_fromEmail, _smtpPassword);
            smtpClient.EnableSsl = true;

            await smtpClient.SendMailAsync(mailMessage);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SMTP ERROR] {ex.Message}");
            return false;
        }
    }

    public async Task<bool> SendOtpEmailAsync(string toEmail, string otpCode)
    {
        string subject = "V-Closet: Mã xác thực OTP đăng ký tài khoản";
        string html = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; padding: 20px; border: 1px solid #eee; border-radius: 10px;'>
                <h2 style='color: #4F46E5; text-align: center;'>Chào mừng bạn đến với V-Closet!</h2>
                <p>Cảm ơn bạn đã đăng ký tài khoản. Vui lòng sử dụng mã OTP dưới đây để hoàn tất kích hoạt tài khoản:</p>
                <div style='text-align: center; margin: 30px 0;'>
                    <span style='font-size: 32px; font-weight: bold; letter-spacing: 5px; color: #4F46E5; background-color: #EEF2F6; padding: 10px 20px; border-radius: 5px;'>{otpCode}</span>
                </div>
                <p style='color: #666; font-size: 13px;'>Mã OTP này có hiệu lực trong vòng 5 phút. Vui lòng không chia sẻ mã này cho bất kỳ ai.</p>
            </div>";
        return await SendEmailAsync(toEmail, subject, html);
    }

    public async Task<bool> SendForgotPasswordOtpEmailAsync(string toEmail, string otpCode)
    {
        string subject = "V-Closet: Mã xác thực OTP đặt lại mật khẩu";
        string html = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; padding: 20px; border: 1px solid #eee; border-radius: 10px;'>
                <h2 style='color: #4F46E5;'>Yêu cầu đặt lại mật khẩu</h2>
                <p>Chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản V-Closet của bạn. Vui lòng sử dụng mã OTP dưới đây để xác nhận yêu cầu của bạn:</p>
                <div style='text-align: center; margin: 30px 0;'>
                    <span style='font-size: 32px; font-weight: bold; letter-spacing: 5px; color: #4F46E5; background-color: #EEF2F6; padding: 10px 20px; border-radius: 5px;'>{otpCode}</span>
                </div>
                <p style='color: #666; font-size: 13px;'>Mã OTP này có hiệu lực trong vòng 15 phút. Nếu bạn không yêu cầu đổi mật khẩu, vui lòng bỏ qua email này để bảo vệ tài khoản.</p>
            </div>";
        return await SendEmailAsync(toEmail, subject, html);
    }
}
