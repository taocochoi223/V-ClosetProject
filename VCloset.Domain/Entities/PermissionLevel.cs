using System;
using System.Collections.Generic;

namespace VCloset.Domain.Entities;

/// <summary>
/// C?p quy?n t?ng th? cho admin/moderator. FK t? admin_profiles.
/// </summary>
public partial class PermissionLevel
{
    public short Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<AdminProfile> AdminProfiles { get; set; } = new List<AdminProfile>();

    public virtual ICollection<Permission> Permissions { get; set; } = new List<Permission>();
}

