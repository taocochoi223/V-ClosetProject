namespace VCloset.Application.DTOs.Payment.Responses;

public class PayOSPaymentResponse
{
    public string PayUrl { get; set; } = string.Empty;
    public string QrCodeUrl { get; set; } = string.Empty;
}
