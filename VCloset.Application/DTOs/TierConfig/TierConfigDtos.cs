namespace VCloset.Application.DTOs.TierConfig;

/// <summary>Phản hồi cấu hình tier từ Admin API</summary>
public class TierConfigResponse
{
    public string TierName { get; set; } = null!;
    public int BgRemovalCredits { get; set; }
    public int TryOnCredits { get; set; }
    public int? WardrobeItemLimit { get; set; }
    public int? OutfitLimit { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}

/// <summary>Request Admin dùng để cập nhật cấu hình tier</summary>
public class UpdateTierConfigRequest
{
    public int BgRemovalCredits { get; set; }
    public int TryOnCredits { get; set; }
    public int? WardrobeItemLimit { get; set; }
    public int? OutfitLimit { get; set; }
}
