using System;

namespace VCloset.Application.DTOs.Chat.Requests;

public class CreateDirectRoomRequest
{
    public Guid TargetUserId { get; set; }
}
