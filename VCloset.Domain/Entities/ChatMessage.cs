using VCloset.Domain.Enums;
using System.ComponentModel.DataAnnotations.Schema;
using System;
using System.Collections.Generic;

namespace VCloset.Domain.Entities;

/// <summary>
/// L?ch s? tin nh?n. Share outfit v�o chat. Soft delete d? moderator ki?m duy?t.
/// </summary>
public partial class ChatMessage
{
    public long InternalId { get; set; }

    public Guid Id { get; set; }

    public int RoomInternalId { get; set; }

    public int UserInternalId { get; set; }

    public string? Content { get; set; }

    public int? OutfitInternalId { get; set; }

    public string? ImageUrl { get; set; }

    public DateTime? DeletedAt { get; set; }

    public DateTime SentAt { get; set; }

    public virtual CanvasOutfit? OutfitInternal { get; set; }

    public virtual ChatRoom RoomInternal { get; set; } = null!;

    public virtual User UserInternal { get; set; } = null!;
    [Column("message_type")]
    public MessageType MessageType { get; set; }
}

