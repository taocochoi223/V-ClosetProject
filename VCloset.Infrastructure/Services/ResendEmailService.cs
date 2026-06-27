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
                from = "V-Closet <noreply@vcloset.vn>",
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

    private string GetBaseEmailTemplate(string content)
    {
        return $@"
            <div style='font-family: ""Helvetica Neue"", Helvetica, Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 0; border: 1px solid #e5e7eb; border-radius: 8px; overflow: hidden;'>
                <!-- Header -->
                <div style='background-color: #4F46E5; padding: 20px; text-align: center;'>
                    <h1 style='color: #ffffff; margin: 0; font-size: 24px; letter-spacing: 1px;'>V-CLOSET</h1>
                </div>
                
                <!-- Body -->
                <div style='padding: 30px; background-color: #ffffff; color: #1f2937;'>
                    {content}
                </div>
                
                <!-- Footer -->
                <div style='background-color: #f9fafb; padding: 20px; text-align: center; border-top: 1px solid #e5e7eb;'>
                    <p style='margin: 0; color: #6b7280; font-size: 14px;'>Bạn cần hỗ trợ? Vui lòng liên hệ với chúng tôi qua:</p>
                    <p style='margin: 8px 0; font-size: 14px;'>
                        <a href='mailto:support@vcloset.vn' style='color: #4F46E5; text-decoration: none; font-weight: bold;'>support@vcloset.vn</a>
                        &nbsp;|&nbsp;
                        <a href='https://www.facebook.com/profile.php?id=61590136782776' target='_blank' style='color: #4F46E5; text-decoration: none; font-weight: bold;'>Fanpage Facebook</a>
                    </p>
                    <p style='margin: 15px 0 0 0; color: #9ca3af; font-size: 12px;'>
                        &copy; {DateTime.UtcNow.Year} V-Closet. All rights reserved.
                    </p>
                </div>
            </div>";
    }

    public async Task<bool> SendOtpEmailAsync(string toEmail, string otpCode)
    {
        string subject = "V-Closet: Mã xác thực OTP đăng ký tài khoản";
        string content = $@"
            <h2 style='color: #111827; text-align: center; margin-top: 0;'>Chào mừng bạn đến với <span style='color: #4F46E5;'>V-Closet</span>!</h2>
            <p style='font-size: 15px; line-height: 1.6;'>Cảm ơn bạn đã đăng ký tài khoản. Vui lòng sử dụng mã OTP dưới đây để hoàn tất kích hoạt tài khoản của bạn:</p>
            <div style='text-align: center; margin: 30px 0;'>
                <span style='font-size: 32px; font-weight: bold; letter-spacing: 5px; color: #4F46E5; background-color: #EEF2F6; padding: 12px 24px; border-radius: 8px; display: inline-block;'>{otpCode}</span>
            </div>
            <p style='color: #6b7280; font-size: 13px; text-align: center;'>Mã OTP này có hiệu lực trong vòng 5 phút.<br>Vui lòng không chia sẻ mã này cho bất kỳ ai để bảo vệ tài khoản.</p>";

        string html = GetBaseEmailTemplate(content);
        return await SendEmailAsync(toEmail, subject, html);
    }

    public async Task<bool> SendForgotPasswordOtpEmailAsync(string toEmail, string otpCode)
    {
        string subject = "V-Closet: Mã xác thực OTP đặt lại mật khẩu";
        string content = $@"
            <h2 style='color: #111827; text-align: center; margin-top: 0;'>Yêu cầu đặt lại mật khẩu</h2>
            <p style='font-size: 15px; line-height: 1.6;'>Chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản V-Closet của bạn. Vui lòng sử dụng mã OTP dưới đây để xác nhận:</p>
            <div style='text-align: center; margin: 30px 0;'>
                <span style='font-size: 32px; font-weight: bold; letter-spacing: 5px; color: #4F46E5; background-color: #EEF2F6; padding: 12px 24px; border-radius: 8px; display: inline-block;'>{otpCode}</span>
            </div>
            <p style='color: #6b7280; font-size: 13px; text-align: center;'>Mã OTP này có hiệu lực trong vòng 15 phút.<br>Nếu bạn không yêu cầu đổi mật khẩu, vui lòng bỏ qua email này.</p>";

        string html = GetBaseEmailTemplate(content);
        return await SendEmailAsync(toEmail, subject, html);
    }

    public async Task<bool> SendNewAccountEmailAsync(string toEmail, string displayName, string tempPassword)
    {
        string subject = "V-Closet: Thông tin tài khoản mới";
        string content = $@"
            <h2 style='color: #111827; text-align: center; margin-top: 0;'>Chào <span style='color: #4F46E5;'>{displayName}</span>,</h2>
            <p style='font-size: 15px; line-height: 1.6;'>Tài khoản của bạn đã được tạo thành công bởi quản trị viên hệ thống V-Closet. Dưới đây là thông tin đăng nhập của bạn:</p>
            
            <div style='background-color: #f3f4f6; padding: 20px; border-radius: 8px; margin: 25px 0;'>
                <p style='margin: 0 0 10px 0;'><strong>Tài khoản:</strong> {toEmail}</p>
                <p style='margin: 0;'><strong>Mật khẩu tạm thời:</strong> <code style='font-size: 16px; color: #d63384; font-family: monospace; background: #fff; padding: 4px 8px; border-radius: 4px; font-weight: bold;'>{tempPassword}</code></p>
            </div>
            
            <p style='color: #6b7280; font-size: 14px;'>Vui lòng đăng nhập và thực hiện đổi mật khẩu ngay để đảm bảo an toàn bảo mật.</p>";

        string html = GetBaseEmailTemplate(content);
        return await SendEmailAsync(toEmail, subject, html);
    }

    public async Task<bool> SendAdminPaymentNotificationAsync(string toEmail, string senderName, string senderEmail, string planName, decimal amount, string currency, string userNote, DateTime createdAt)
    {
        string subject = "V-Closet: Thông báo giao dịch chuyển khoản mới";
        string content = $@"
            <h2 style='color: #111827; text-align: center; margin-top: 0;'>Yêu cầu duyệt giao dịch</h2>
            <p style='font-size: 15px; line-height: 1.6;'>Có một giao dịch nạp tiền hoặc thanh toán mới đang chờ bạn xét duyệt.</p>
            
            <table style='width: 100%; border-collapse: collapse; margin: 25px 0; font-size: 14px;'>
                <tr>
                    <td style='padding: 12px; border: 1px solid #e5e7eb; background-color: #f9fafb; font-weight: bold; width: 40%;'>Người gửi:</td>
                    <td style='padding: 12px; border: 1px solid #e5e7eb;'>{senderName} (<a href='mailto:{senderEmail}' style='color: #4F46E5;'>{senderEmail}</a>)</td>
                </tr>
                <tr>
                    <td style='padding: 12px; border: 1px solid #e5e7eb; background-color: #f9fafb; font-weight: bold;'>Gói đăng ký:</td>
                    <td style='padding: 12px; border: 1px solid #e5e7eb;'>{planName}</td>
                </tr>
                <tr>
                    <td style='padding: 12px; border: 1px solid #e5e7eb; background-color: #f9fafb; font-weight: bold;'>Số tiền:</td>
                    <td style='padding: 12px; border: 1px solid #e5e7eb;'>{amount:N0} {currency}</td>
                </tr>
                <tr>
                    <td style='padding: 12px; border: 1px solid #e5e7eb; background-color: #f9fafb; font-weight: bold;'>Ghi chú của User:</td>
                    <td style='padding: 12px; border: 1px solid #e5e7eb;'>{(string.IsNullOrEmpty(userNote) ? "Không có" : userNote)}</td>
                </tr>
                <tr>
                    <td style='padding: 12px; border: 1px solid #e5e7eb; background-color: #f9fafb; font-weight: bold;'>Thời gian gửi:</td>
                    <td style='padding: 12px; border: 1px solid #e5e7eb;'>{createdAt.AddHours(7):dd/MM/yyyy HH:mm:ss} (Giờ VN)</td>
                </tr>
            </table>
            
            <p style='font-size: 14px; text-align: center; margin-bottom: 25px;'>Vui lòng đăng nhập vào hệ thống để kiểm tra và đối soát chứng từ.</p>
            
            <div style='text-align: center;'>
                <a href='https://admin.vcloset.vn/admin/subscriptions' style='background-color: #4F46E5; color: #ffffff; padding: 12px 24px; text-decoration: none; font-weight: bold; border-radius: 6px; display: inline-block;'>Đến Trang Quản Trị</a>
            </div>";

        string html = GetBaseEmailTemplate(content);
        return await SendEmailAsync(toEmail, subject, html);
    }

    public async Task<bool> SendAccountDeactivatedEmailAsync(string toEmail, string displayName)
    {
        string subject = "V-Closet: Thông báo trạng thái tài khoản";
        string content = $@"
            <h2 style='color: #dc2626; text-align: center; margin-top: 0;'>Thông báo tạm ngưng tài khoản</h2>
            <p style='font-size: 15px; line-height: 1.6;'>Chào <span style='font-weight: bold;'>{displayName}</span>,</p>
            <p style='font-size: 15px; line-height: 1.6;'>Chúng tôi rất tiếc phải thông báo rằng tài khoản V-Closet của bạn hiện tại đã bị vô hiệu hóa (inActive).</p>
            
            <div style='background-color: #fef2f2; border-left: 4px solid #ef4444; padding: 15px; margin: 25px 0;'>
                <p style='margin: 0; color: #991b1b; font-size: 14px;'>Tài khoản của bạn sẽ tạm thời không thể đăng nhập hoặc sử dụng các dịch vụ thử đồ AI tại V-Closet.</p>
            </div>
            
            <p style='color: #4b5563; font-size: 14px; text-align: center;'>Nếu bạn cho rằng đây là một sự nhầm lẫn, vui lòng liên hệ với Đội ngũ hỗ trợ của chúng tôi để được giải đáp.</p>";

        string html = GetBaseEmailTemplate(content);
        return await SendEmailAsync(toEmail, subject, html);
    }

    public async Task<bool> SendAccountReactivatedEmailAsync(string toEmail, string displayName)
    {
        string subject = "V-Closet: Tài khoản của bạn đã được kích hoạt lại";
        string content = $@"
            <h2 style='color: #10b981; text-align: center; margin-top: 0;'>Tài khoản đã được kích hoạt</h2>
            <p style='font-size: 15px; line-height: 1.6;'>Chào <span style='font-weight: bold;'>{displayName}</span>,</p>
            <p style='font-size: 15px; line-height: 1.6;'>Chúng tôi vui mừng thông báo tài khoản V-Closet của bạn đã được kích hoạt lại thành công.</p>
            
            <div style='background-color: #f0fdf4; border-left: 4px solid #10b981; padding: 15px; margin: 25px 0;'>
                <p style='margin: 0; color: #065f46; font-size: 14px;'>Bây giờ bạn đã có thể đăng nhập bình thường và tiếp tục trải nghiệm các tính năng thử đồ AI tuyệt vời tại V-Closet.</p>
            </div>
            
            <div style='text-align: center; margin-top: 30px;'>
                <a href='https://vcloset.vn' style='background-color: #4F46E5; color: #ffffff; padding: 12px 24px; text-decoration: none; font-weight: bold; border-radius: 6px; display: inline-block;'>Đăng nhập ngay</a>
            </div>";

        string html = GetBaseEmailTemplate(content);
        return await SendEmailAsync(toEmail, subject, html);
    }
    public async Task<bool> SendPaymentReceiptEmailAsync(string toEmail, string customerName, string planName, decimal amount, string transactionId, DateTime paymentDate)
    {
        string subject = "V-Closet: Biên lai thanh toán thành công";
        string content = $@"
            <div style='text-align: center; margin-bottom: 20px;'>
                <div style='background-color: #10B981; color: white; display: inline-block; padding: 10px 20px; border-radius: 50px; font-weight: bold; margin-bottom: 10px;'>
                    ✔ Thanh toán thành công
                </div>
                <h2 style='color: #111827; margin-top: 0;'>Biên lai điện tử</h2>
            </div>
            <p style='font-size: 15px; line-height: 1.6;'>Chào <strong>{customerName}</strong>,</p>
            <p style='font-size: 15px; line-height: 1.6;'>Cảm ơn bạn đã sử dụng dịch vụ của V-Closet. Giao dịch thanh toán của bạn đã được xác nhận thành công. Dưới đây là thông tin chi tiết:</p>
            
            <table style='width: 100%; border-collapse: collapse; margin: 25px 0; font-size: 14px;'>
                <tr>
                    <td style='padding: 12px; border-bottom: 1px solid #e5e7eb; color: #6b7280; width: 40%;'>Mã giao dịch</td>
                    <td style='padding: 12px; border-bottom: 1px solid #e5e7eb; font-weight: bold; text-align: right;'>{transactionId}</td>
                </tr>
                <tr>
                    <td style='padding: 12px; border-bottom: 1px solid #e5e7eb; color: #6b7280;'>Gói dịch vụ</td>
                    <td style='padding: 12px; border-bottom: 1px solid #e5e7eb; font-weight: bold; text-align: right;'>{planName}</td>
                </tr>
                <tr>
                    <td style='padding: 12px; border-bottom: 1px solid #e5e7eb; color: #6b7280;'>Số tiền thanh toán</td>
                    <td style='padding: 12px; border-bottom: 1px solid #e5e7eb; font-weight: bold; text-align: right; color: #4a3728;'>{amount:N0} VNĐ</td>
                </tr>
                <tr>
                    <td style='padding: 12px; border-bottom: 1px solid #e5e7eb; color: #6b7280;'>Thời gian</td>
                    <td style='padding: 12px; border-bottom: 1px solid #e5e7eb; font-weight: bold; text-align: right;'>{paymentDate.AddHours(7):dd/MM/yyyy HH:mm:ss}</td>
                </tr>
            </table>
            
            <p style='color: #6b7280; font-size: 14px; text-align: center;'>Gói dịch vụ của bạn đã được kích hoạt. Hãy trải nghiệm ngay những tính năng cao cấp trên ứng dụng V-Closet!</p>";

        string html = GetBaseEmailTemplate(content);
        return await SendEmailAsync(toEmail, subject, html);
    }

    public async Task<bool> SendSystemNotificationEmailAsync(string toEmail, string displayName, string subject, string bodyContent)
    {
        string content = $@"
            <h2 style='color: #4F46E5; text-align: center; margin-top: 0;'>Thông báo từ V-Closet</h2>
            <p style='font-size: 15px; line-height: 1.6;'>Chào <span style='font-weight: bold;'>{displayName}</span>,</p>
            <div style='background-color: #f9fafb; padding: 15px; border-radius: 8px; margin: 20px 0; border: 1px solid #e5e7eb;'>
                <p style='margin: 0; font-size: 15px; line-height: 1.6; white-space: pre-wrap;'>{bodyContent}</p>
            </div>
            <p style='color: #6b7280; font-size: 14px; text-align: center;'>Vui lòng mở ứng dụng hoặc truy cập website V-Closet để xem thêm chi tiết.</p>";

        string html = GetBaseEmailTemplate(content);
        return await SendEmailAsync(toEmail, subject, html);
    }
}
