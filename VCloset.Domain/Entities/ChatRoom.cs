using VCloset.Domain.Enums;
using System.ComponentModel.DataAnnotations.Schema;
using System;
using System.Collections.Generic;

namespace VCloset.Domain.Entities;

/// <summary>
/// Ph�ng chat: public, topic (theo ch? d? th?i trang), direct (2 ngu?i).
/// </summary>
public partial class ChatRoom
{
    public int InternalId { get; set; }

    public Guid Id { get; set; }

    public string? Name { get; set; }

    public string? Description { get; set; }

    public string? CoverUrl { get; set; }

    public bool IsActive { get; set; }

    public int? CreatedByInternal { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();

    public virtual ICollection<ChatRoomMember> ChatRoomMembers { get; set; } = new List<ChatRoomMember>();

    public virtual User? CreatedByInternalNavigation { get; set; }
    [Column("room_type")]
    public ChatRoomType RoomType { get; set; }
}

