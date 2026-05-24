using System;

namespace VCloset.Application.DTOs.Users.Responses;

public class FollowerResponse
{
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = null!;
    public string? AvatarUrl { get; set; }
}
