namespace VCloset.Application.DTOs.Admin.Requests;

public class SaveDeviceTokenRequest
{
    public string FcmToken { get; set; } = null!;
    public string DeviceType { get; set; } = null!; // "iOS", "Android", "Web"
}
