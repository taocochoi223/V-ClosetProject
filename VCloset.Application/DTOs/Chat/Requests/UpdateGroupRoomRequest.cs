using System;
using System.ComponentModel.DataAnnotations;

namespace VCloset.Application.DTOs.Chat.Requests;

public class UpdateGroupRoomRequest
{
    [MaxLength(255)]
    public string? Name { get; set; }

    [MaxLength(2000)]
    public string? CoverUrl { get; set; }
}
