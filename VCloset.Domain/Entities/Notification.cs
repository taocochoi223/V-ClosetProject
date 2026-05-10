using System;
using System.Collections.Generic;

namespace VCloset.Domain.Entities;

/// <summary>
/// Thông báo in-app. reference_id là internal_id c?a object liên quan.
/// </summary>
public partial class Notification
{
    public long InternalId { get; set; }

    public Guid Id { get; set; }

    public int UserInternalId { get; set; }

    public string Type { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string? Body { get; set; }

    public string? ReferenceType { get; set; }

    public int? ReferenceId { get; set; }

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User UserInternal { get; set; } = null!;
}

