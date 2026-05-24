using System;
using System.ComponentModel.DataAnnotations;

namespace VCloset.Application.DTOs.Admin.Requests;

public class BanUserRequest
{
    /// <summary>
    /// Loại ban: "chat" | "post" | "all"
    /// </summary>
    [Required]
    public string BanType { get; set; } = "all";

    /// <summary>
    /// Lý do ban
    /// </summary>
    [Required]
    [MinLength(10, ErrorMessage = "Lý do ban phải có ít nhất 10 ký tự.")]
    public string Reason { get; set; } = null!;

    /// <summary>
    /// Thời điểm hết ban (null = vĩnh viễn)
    /// </summary>
    public DateTime? BannedUntil { get; set; }
}
