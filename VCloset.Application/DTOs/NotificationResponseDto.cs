using System;

namespace VCloset.Application.DTOs;

public class NotificationResponseDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string? Body { get; set; }
    public string? ReferenceType { get; set; }
    public int? ReferenceId { get; set; }
    public bool IsRead { get; set; }
    public int UserInternalId { get; set; }
    public Guid? UserGuid { get; set; }
    public string? UserDisplayName { get; set; }
    public DateTime CreatedAt { get; set; }
}
