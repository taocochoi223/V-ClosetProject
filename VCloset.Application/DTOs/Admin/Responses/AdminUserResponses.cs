using System;

namespace VCloset.Application.DTOs.Admin.Responses;

/// <summary>
/// Thông tin tóm tắt của user dùng trong danh sách admin
/// </summary>
public class AdminUserSummaryResponse
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public string? AvatarUrl { get; set; }
    public string Role { get; set; } = null!;
    public bool IsActive { get; set; }
    public bool IsEmailVerified { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// User đang bị ban hay không (có ban log còn hiệu lực)
    /// </summary>
    public bool IsBanned { get; set; }

    /// <summary>
    /// Loại ban hiện tại nếu đang bị ban
    /// </summary>
    public string? ActiveBanType { get; set; }

    /// <summary>
    /// Thời điểm hết ban (null = vĩnh viễn)
    /// </summary>
    public DateTime? BannedUntil { get; set; }
}

/// <summary>
/// Chi tiết đầy đủ của user khi admin xem
/// </summary>
public class AdminUserDetailResponse : AdminUserSummaryResponse
{
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public string? Gender { get; set; }
    public string? Country { get; set; }
    public decimal? HeightCm { get; set; }
    public decimal? WeightKg { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public int WardrobeItemCount { get; set; }
    public System.Collections.Generic.List<BanLogResponse> BanHistory { get; set; } = new();
}

/// <summary>
/// Lịch sử ban của user
/// </summary>
public class BanLogResponse
{
    public Guid Id { get; set; }
    public string BanType { get; set; } = null!;
    public string Reason { get; set; } = null!;
    public DateTime? BannedUntil { get; set; }
    public bool IsLifted { get; set; }
    public string? LiftReason { get; set; }
    public DateTime? LiftedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string BannedByDisplayName { get; set; } = null!;
}

/// <summary>
/// Kết quả phân trang danh sách user
/// </summary>
public class PagedUsersResponse
{
    public System.Collections.Generic.List<AdminUserSummaryResponse> Users { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
