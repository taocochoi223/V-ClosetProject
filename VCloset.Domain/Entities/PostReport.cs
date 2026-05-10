using System;
using System.Collections.Generic;

namespace VCloset.Domain.Entities;

/// <summary>
/// Report vi ph?m. Moderator xem queue và x? lý t?ng report.
/// </summary>
public partial class PostReport
{
    public Guid Id { get; set; }

    public int PostInternalId { get; set; }

    public int ReporterInternalId { get; set; }

    public string Reason { get; set; } = null!;

    public string? Description { get; set; }

    public bool IsResolved { get; set; }

    public int? ResolvedByInternal { get; set; }

    public DateTime? ResolvedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual CommunityPost PostInternal { get; set; } = null!;

    public virtual User ReporterInternal { get; set; } = null!;

    public virtual User? ResolvedByInternalNavigation { get; set; }
}

