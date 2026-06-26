using VCloset.Domain.Enums;
using System.ComponentModel.DataAnnotations.Schema;
using System;
using System.Collections.Generic;

namespace VCloset.Domain.Entities;

/// <summary>
/// T? d? s?. M?i item c� ?nh g?c v� ?nh d� x�a n?n d? gh�p canvas/mannequin.
/// </summary>
public partial class WardrobeItem
{
    public int InternalId { get; set; }

    public Guid Id { get; set; }

    public int UserInternalId { get; set; }

    public string? Name { get; set; }

    public string OriginalImageUrl { get; set; } = null!;

    public string? RemovedBgUrl { get; set; }

    public List<string>? ColorTags { get; set; }

    public string? Brand { get; set; }

    public string? Notes { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<CanvasOutfitItem> CanvasOutfitItems { get; set; } = new List<CanvasOutfitItem>();

    public virtual User UserInternal { get; set; } = null!;
    [Column("bg_removal_status")]
    public AiJobStatus BgRemovalStatus { get; set; }

    [Column("category")]
    public ClothingCategory Category { get; set; }

    public int? ClosetInternalId { get; set; }

    public virtual Closet? Closet { get; set; }
}

