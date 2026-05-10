using System;
using System.Collections.Generic;

namespace VCloset.Domain.Entities;

/// <summary>
/// Permission c? th? t?ng admin. Composite PK INT. granted_by_internal là audit trail.
/// </summary>
public partial class AdminPermission
{
    public int UserInternalId { get; set; }

    public int PermissionId { get; set; }

    public int GrantedByInternal { get; set; }

    public DateTime GrantedAt { get; set; }

    public virtual User GrantedByInternalNavigation { get; set; } = null!;

    public virtual Permission Permission { get; set; } = null!;

    public virtual User UserInternal { get; set; } = null!;
}

