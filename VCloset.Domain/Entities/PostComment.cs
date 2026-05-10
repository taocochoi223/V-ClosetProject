using System;
using System.Collections.Generic;

namespace VCloset.Domain.Entities;

/// <summary>
/// Bình lu?n bài dang. H? tr? 1 c?p reply qua parent_comment_internal_id.
/// </summary>
public partial class PostComment
{
    public int InternalId { get; set; }

    public Guid Id { get; set; }

    public int PostInternalId { get; set; }

    public int UserInternalId { get; set; }

    public int? ParentCommentInternalId { get; set; }

    public string Content { get; set; } = null!;

    public bool IsHidden { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<PostComment> InverseParentCommentInternal { get; set; } = new List<PostComment>();

    public virtual PostComment? ParentCommentInternal { get; set; }

    public virtual CommunityPost PostInternal { get; set; } = null!;

    public virtual User UserInternal { get; set; } = null!;
}

