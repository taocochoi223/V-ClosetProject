using System;

namespace VCloset.Application.DTOs.Chat.Responses;

public class ChatRoomMemberResponseDto
{
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? AvatarUrl { get; set; }
    public DateTime JoinedAt { get; set; }
    public bool IsAdmin { get; set; } // Nếu là người tạo nhóm
}
