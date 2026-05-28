using System;

namespace VCloset.Domain.Entities;

public class UserDeviceToken
{
    public Guid Id { get; set; }
    public int UserInternalId { get; set; }
    public string FcmToken { get; set; } = null!;
    public string DeviceType { get; set; } = null!; // "iOS", "Android", "Web"
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public virtual User User { get; set; } = null!;
}
