using System;

namespace VCloset.Application.DTOs.Chat.Requests;

public class SendOutfitMessageRequest
{
    public Guid OutfitId { get; set; }
}
