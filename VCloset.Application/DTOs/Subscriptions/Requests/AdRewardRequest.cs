namespace VCloset.Application.DTOs.Subscriptions.Requests;

/// <summary>
/// Request để nhận credit từ việc xem quảng cáo
/// </summary>
public class AdRewardRequest
{
    public string RewardType { get; set; } = string.Empty; // "bg_removal" hoặc "try_on"
}
