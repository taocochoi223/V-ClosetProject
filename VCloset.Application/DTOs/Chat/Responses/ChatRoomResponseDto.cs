using System;
using VCloset.Domain.Enums;

namespace VCloset.Application.DTOs.Chat.Responses;

public class ChatRoomResponseDto
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? CoverUrl { get; set; }
    public ChatRoomType RoomType { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Preview fields for chat list
    public string? LastMessageContent { get; set; }
    public DateTime? LastMessageSentAt { get; set; }
    public string? LastMessageSenderName { get; set; }
    public int UnreadCount { get; set; }
}
