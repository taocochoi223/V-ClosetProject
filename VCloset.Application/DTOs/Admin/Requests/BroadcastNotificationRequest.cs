namespace VCloset.Application.DTOs.Admin.Requests;

public class BroadcastNotificationRequest
{
    public string Title { get; set; } = null!;
    public string Body { get; set; } = null!;
    public string? Type { get; set; } // Mặc định: "System"
    public string? ReferenceType { get; set; }
    public int? ReferenceId { get; set; }
}
