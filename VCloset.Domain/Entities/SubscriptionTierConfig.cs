using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace VCloset.Domain.Entities;

/// <summary>
/// Cấu hình giới hạn (credits, wardrobe limit, outfit limit) theo từng tier gói dịch vụ.
/// Admin có thể chỉnh trực tiếp qua API mà không cần deploy lại code.
/// </summary>
public partial class SubscriptionTierConfig
{
    public int InternalId { get; set; }

    /// <summary>"free" hoặc "premium"</summary>
    [Column("tier_name")]
    public string TierName { get; set; } = null!;

    /// <summary>Số lượt tách nền AI được cấp khi kích hoạt tier này</summary>
    [Column("bg_removal_credits")]
    public int BgRemovalCredits { get; set; }

    /// <summary>Số lượt thử đồ AI được cấp khi kích hoạt tier này</summary>
    [Column("try_on_credits")]
    public int TryOnCredits { get; set; }

    /// <summary>Giới hạn số món đồ trong tủ (null = không giới hạn)</summary>
    [Column("wardrobe_item_limit")]
    public int? WardrobeItemLimit { get; set; }

    /// <summary>Giới hạn số bộ phối đồ Canvas (null = không giới hạn)</summary>
    [Column("outfit_limit")]
    public int? OutfitLimit { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [Column("updated_by")]
    public string? UpdatedBy { get; set; }
}
