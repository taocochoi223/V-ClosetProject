using VCloset.Domain.Enums;
using System.ComponentModel.DataAnnotations.Schema;
using System;
using System.Collections.Generic;

namespace VCloset.Domain.Entities;

/// <summary>
/// Profile customer: s? do, mannequin AI, tr?ng th�i ban. FK d�ng INT.
/// </summary>
public partial class CustomerProfile
{
    public int InternalId { get; set; }

    public Guid Id { get; set; }

    public int UserInternalId { get; set; }

    public decimal? HeightCm { get; set; }

    public decimal? WeightKg { get; set; }

    public string? MannequinImageUrl { get; set; }

    public DateTime? MannequinGeneratedAt { get; set; }

    /// <summary>
    /// Cache d? check gi?i h?n freemium 50 items m� kh�ng COUNT(*).
    /// </summary>
    public int WardrobeItemCount { get; set; }

    /// <summary>
    /// TRUE = b? kho� chat. K?t h?p chat_banned_until ph�n bi?t t?m th?i/vinh vi?n.
    /// </summary>
    public bool IsChatBanned { get; set; }

    /// <summary>
    /// TRUE = b? kho� dang b�i. K?t h?p post_banned_until ph�n bi?t t?m th?i/vinh vi?n.
    /// </summary>
    public bool IsPostBanned { get; set; }

    public DateTime? ChatBannedUntil { get; set; }

    public DateTime? PostBannedUntil { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual User UserInternal { get; set; } = null!;
    [Column("body_shape")]
    public BodyShapeType? BodyShape { get; set; }
}

