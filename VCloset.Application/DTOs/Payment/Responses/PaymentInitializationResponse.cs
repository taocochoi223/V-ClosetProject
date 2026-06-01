namespace VCloset.Application.DTOs.Payment.Responses;

public class PaymentInitializationResponse
{
    public string PayUrl { get; set; } = string.Empty;
    public string PaymentGateway { get; set; } = string.Empty;
    public string? Deeplink { get; set; }
    public string? QrCodeUrl { get; set; }
}
