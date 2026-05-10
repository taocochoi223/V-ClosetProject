using System;
using System.Collections.Generic;

namespace VCloset.Domain.Entities;

/// <summary>
/// Like bài dang. Composite PK INT d?m b?o 1 user ch? like 1 bài 1 l?n.
/// </summary>
public partial class PostLike
{
    public int PostInternalId { get; set; }

    public int UserInternalId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual CommunityPost PostInternal { get; set; } = null!;

    public virtual User UserInternal { get; set; } = null!;
}

