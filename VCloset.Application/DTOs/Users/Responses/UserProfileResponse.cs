using System;

namespace VCloset.Application.DTOs;

public class UserProfileResponse
{
    // Thông tin cơ bản từ bảng User
    public int UserId { get; set; }
    public string Email { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public string? AvatarUrl { get; set; }
    public string Role { get; set; } = null!;

    // Thông tin chi tiết từ bảng CustomerProfile
    public decimal? HeightCm { get; set; }
    public decimal? WeightKg { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public string? Gender { get; set; }
    public string? Country { get; set; }
    
    // Thuộc tính phục vụ cho thời trang / AI
    public string? BodyShape { get; set; }
    public string? MannequinImageUrl { get; set; }
    public int WardrobeItemCount { get; set; }
    public bool IsOnboardingCompleted { get; set; }
    public string? Lifestyle { get; set; }
    public string? EyeColor { get; set; }
    public string? Hair { get; set; }
}
