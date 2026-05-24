namespace VCloset.Application.DTOs.Users.Responses;

public class PublicProfileResponse
{
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = null!;
    public string? AvatarUrl { get; set; }
    public decimal? HeightCm { get; set; }
    public decimal? WeightKg { get; set; }
    public string? Gender { get; set; }
    public int WardrobeItemCount { get; set; }
}