using System;
using System.Collections.Generic;

namespace VCloset.Domain.Entities;

/// <summary>
/// Danh m?c permission. code d?ng group.action dùng trong C# RequirePermission attribute.
/// </summary>
public partial class Permission
{
    public int Id { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string Grp { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<AdminPermission> AdminPermissions { get; set; } = new List<AdminPermission>();

    public virtual ICollection<PermissionLevel> PermissionLevels { get; set; } = new List<PermissionLevel>();
}

