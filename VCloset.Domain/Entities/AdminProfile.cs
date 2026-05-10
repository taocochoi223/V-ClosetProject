using System;
using System.Collections.Generic;

namespace VCloset.Domain.Entities;

/// <summary>
/// Profile admin/moderator. permission_level là vai trò t?ng th?, chi ti?t ? admin_permissions.
/// </summary>
public partial class AdminProfile
{
    public int InternalId { get; set; }

    public Guid Id { get; set; }

    public int UserInternalId { get; set; }

    public short PermissionLevel { get; set; }

    public string? Department { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual PermissionLevel PermissionLevelNavigation { get; set; } = null!;

    public virtual User UserInternal { get; set; } = null!;
}

