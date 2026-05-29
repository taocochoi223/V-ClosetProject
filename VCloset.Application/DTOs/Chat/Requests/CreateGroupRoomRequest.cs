using System;
using System.Collections.Generic;

namespace VCloset.Application.DTOs.Chat.Requests;

public class CreateGroupRoomRequest
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? CoverUrl { get; set; }
    public List<Guid> MemberUserIds { get; set; } = new();
}
