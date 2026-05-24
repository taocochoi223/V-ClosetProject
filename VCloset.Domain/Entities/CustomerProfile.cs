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

    public DateTime? DateOfBirth { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Address { get; set; }

    public string? Gender { get; set; }

    public string? MannequinImageUrl { get; set; }

    public DateTime? MannequinGeneratedAt { get; set; }

    public int WardrobeItemCount { get; set; }

    public bool IsChatBanned { get; set; }

    public bool IsPostBanned { get; set; }

    public DateTime? ChatBannedUntil { get; set; }

    public DateTime? PostBannedUntil { get; set; }

    public bool IsOnboardingCompleted { get; set; }
    
    public string? Country { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual User UserInternal { get; set; } = null!;
    [Column("body_shape")]
    public BodyShapeType? BodyShape { get; set; }
}

