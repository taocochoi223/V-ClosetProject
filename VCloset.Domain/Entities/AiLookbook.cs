using VCloset.Domain.Enums;
using System.ComponentModel.DataAnnotations.Schema;
using System;
using System.Collections.Generic;

namespace VCloset.Domain.Entities;

/// <summary>
/// ?nh lookbook AI generate t? canvas outfit. Luu prompt d? A/B test c?i thi?n model.
/// </summary>
public partial class AiLookbook
{
    public int InternalId { get; set; }

    public Guid Id { get; set; }

    public int OutfitInternalId { get; set; }

    public int UserInternalId { get; set; }

    public string? GeneratedImageUrl { get; set; }

    public string? AiPromptUsed { get; set; }

    public string? ErrorMessage { get; set; }

    public decimal? GenerationSeconds { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual CanvasOutfit OutfitInternal { get; set; } = null!;

    public virtual User UserInternal { get; set; } = null!;
    [Column("status")]
    public AiJobStatus Status { get; set; }
}

