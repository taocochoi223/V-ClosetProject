using System;

namespace VCloset.Application.DTOs.Subscriptions.Responses;

/// <summary>
/// Trạng thái gói Premium của user hiện tại + số credits còn lại
/// </summary>
public class MySubscriptionResponse
{
    public bool HasActivePremium { get; set; }
    public string? PlanName { get; set; }
    public string? PlanType { get; set; }      // "monthly" | "yearly"
    public DateTime? ExpiresAt { get; set; }
    public int DaysRemaining { get; set; }

    // Credits
    public int BgRemovalCredits { get; set; }
    public int TryOnCredits { get; set; }

    // Wardrobe limits
    public int WardrobeItemCount { get; set; }
    public int? WardrobeItemLimit { get; set; }  // null = không giới hạn

    // Outfit limits
    public int OutfitCount { get; set; }
    public int? OutfitLimit { get; set; }  // null = không giới hạn
}
