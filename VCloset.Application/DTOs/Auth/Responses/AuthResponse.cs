namespace VCloset.Application.DTOs;

public class AuthResponse
{
    public string AccessToken { get; set; } = null!;
    public string RefreshToken { get; set; } = null!;
    public int UserId { get; set; }
    public string Role { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public string? AvatarUrl { get; set; }
    public bool IsOnboardingCompleted { get; set; }
    public bool IsPasswordSet { get; set; }
    public bool HasActivePremium { get; set; }
    public string PlanType { get; set; } = "free";
}
