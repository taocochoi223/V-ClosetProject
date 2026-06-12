using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace VCloset.Application.DTOs.Chat.Requests;

public class AddGroupMembersRequest
{
    [Required]
    public List<Guid> MemberUserIds { get; set; } = new List<Guid>();
}
