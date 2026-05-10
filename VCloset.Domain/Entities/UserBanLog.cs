using System;
using System.Collections.Generic;

namespace VCloset.Domain.Entities;

/// <summary>
/// L?ch s? khoá/m? khoá. Audit log d? moderator gi?i trình và xem pattern vi ph?m.
/// </summary>
public partial class UserBanLog
{
    public Guid Id { get; set; }

    public int UserInternalId { get; set; }

    public int BannedByInternal { get; set; }

    public string BanType { get; set; } = null!;

    public string Reason { get; set; } = null!;

    public DateTime? BannedUntil { get; set; }

    public bool IsLifted { get; set; }

    public int? LiftedByInternal { get; set; }

    public DateTime? LiftedAt { get; set; }

    public string? LiftReason { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User BannedByInternalNavigation { get; set; } = null!;

    public virtual User? LiftedByInternalNavigation { get; set; }

    public virtual User UserInternal { get; set; } = null!;
}

