using System;
using System.Collections.Generic;

namespace VCloset.Domain.Entities;

/// <summary>
/// Bài dang community feed. G?n v?i canvas outfit d? ngu?i khác th? outfit tuong t?.
/// </summary>
public partial class CommunityPost
{
    public int InternalId { get; set; }

    public Guid Id { get; set; }

    public int UserInternalId { get; set; }

    public int? OutfitInternalId { get; set; }

    public string? Caption { get; set; }

    public int LikeCount { get; set; }

    public int CommentCount { get; set; }

    public bool IsPublic { get; set; }

    public bool IsHidden { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual CanvasOutfit? OutfitInternal { get; set; }

    public virtual ICollection<PostComment> PostComments { get; set; } = new List<PostComment>();

    public virtual ICollection<PostLike> PostLikes { get; set; } = new List<PostLike>();

    public virtual ICollection<PostReport> PostReports { get; set; } = new List<PostReport>();

    public virtual User UserInternal { get; set; } = null!;
}

