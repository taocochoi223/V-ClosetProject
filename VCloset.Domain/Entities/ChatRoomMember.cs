using System;
using System.Collections.Generic;

namespace VCloset.Domain.Entities;

/// <summary>
/// Thành viên phòng chat. last_read_at dùng hi?n th? s? tin chua d?c.
/// </summary>
public partial class ChatRoomMember
{
    public int RoomInternalId { get; set; }

    public int UserInternalId { get; set; }

    public DateTime JoinedAt { get; set; }

    public DateTime? LastReadAt { get; set; }

    public bool IsMuted { get; set; }

    public virtual ChatRoom RoomInternal { get; set; } = null!;

    public virtual User UserInternal { get; set; } = null!;
}

