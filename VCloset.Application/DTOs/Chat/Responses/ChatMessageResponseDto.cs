using System;
using VCloset.Domain.Enums;

namespace VCloset.Application.DTOs.Chat.Responses;

public class ChatMessageResponseDto
{
    public Guid Id { get; set; }
    public Guid RoomId { get; set; }
    public Guid SenderId { get; set; }
    public string SenderName { get; set; } = null!;
    public string? SenderAvatarUrl { get; set; }
    public string? Content { get; set; }
    public string? ImageUrl { get; set; }
    public Guid? OutfitId { get; set; }
    public string? OutfitName { get; set; }
    public string? OutfitImageUrl { get; set; }
    public MessageType MessageType { get; set; }
    public DateTime SentAt { get; set; }
}
