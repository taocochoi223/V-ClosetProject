using System;
using System.Collections.Generic;

namespace VCloset.Domain.Entities;

/// <summary>
/// Thư mục tủ đồ tự tạo của người dùng.
/// </summary>
public partial class Closet
{
    public int InternalId { get; set; }

    public Guid Id { get; set; } = Guid.NewGuid();

    public int UserInternalId { get; set; }

    public string Name { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public virtual User UserInternal { get; set; } = null!;

    public virtual ICollection<WardrobeItem> WardrobeItems { get; set; } = new List<WardrobeItem>();
}
