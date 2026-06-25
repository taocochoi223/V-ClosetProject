namespace VCloset.Application.DTOs.Admin.Requests;

public class SendTargetedNotificationRequest
{
    public int UserId { get; set; }
    public string Title { get; set; } = null!;
    public string Body { get; set; } = null!;
    public string? Type { get; set; } // Mặc định "System"
    public string? ReferenceType { get; set; }
    public int? ReferenceId { get; set; }
    public bool SendViaApp { get; set; } = true;
    public bool SendViaEmail { get; set; } = false;
}
