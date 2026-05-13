using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using VCloset.Application.Interfaces;

namespace VCloset.Infrastructure.Services;

public class ResendEmailService : IEmailService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public ResendEmailService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _apiKey = Environment.GetEnvironmentVariable("RESEND_API_KEY") ?? string.Empty;
    }

    public async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlContent)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            Console.WriteLine("[ERROR] RESEND_API_KEY is not configured in .env file.");
            return false;
        }

        try
        {
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails");
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            var payload = new
            {
                from = "V-Closet <onboarding@resend.dev>",
                to = new[] { toEmail },
                subject = subject,
                html = htmlContent
            };

            var json = JsonSerializer.Serialize(payload);
            requestMessage.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(requestMessage);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[RESEND ERROR] Status Code: {response.StatusCode}, Response: {errorBody}");
            }
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EMAIL ERROR] {ex.Message}");
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

    public async Task<bool> SendPasswordResetLinkAsync(string toEmail, string resetLink)
    {
        string subject = "V-Closet: Yêu cầu đặt lại mật khẩu";
        string html = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; padding: 20px; border: 1px solid #eee; border-radius: 10px;'>
                <h2 style='color: #4F46E5;'>Yêu cầu đặt lại mật khẩu</h2>
                <p>Chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản V-Closet của bạn. Vui lòng nhấn vào nút dưới đây để thiết lập mật khẩu mới:</p>
                <div style='text-align: center; margin: 30px 0;'>
                    <a href='{resetLink}' style='background-color: #4F46E5; color: white; padding: 12px 25px; text-decoration: none; font-weight: bold; border-radius: 5px; display: inline-block;'>Đặt lại mật khẩu</a>
                </div>
                <p style='color: #666; font-size: 13px;'>Đường dẫn này có hiệu lực trong vòng 15 phút. Nếu bạn không yêu cầu đổi mật khẩu, vui lòng bỏ qua email này.</p>
            </div>";
        return await SendEmailAsync(toEmail, subject, html);
    }
}
