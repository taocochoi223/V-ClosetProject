namespace VCloset.Application.DTOs.Payment.Responses;

public class MoMoPaymentResponseDto
{
    public string PayUrl { get; set; } = string.Empty;
    public string Deeplink { get; set; } = string.Empty;
    public string QrCodeUrl { get; set; } = string.Empty;
}
