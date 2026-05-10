using System;
using System.Collections.Generic;
using System.Net;

namespace VCloset.Domain.Entities;

/// <summary>
/// JWT refresh token theo thi?t b?. Logout t? xa, revoke token b?t thu?ng.
/// </summary>
public partial class RefreshToken
{
    public Guid Id { get; set; }

    public int UserInternalId { get; set; }

    public string TokenHash { get; set; } = null!;

    public string? DeviceInfo { get; set; }

    public IPAddress? IpAddress { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User UserInternal { get; set; } = null!;
}

