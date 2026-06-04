using System;

namespace VCloset.Application.DTOs.Subscriptions.Responses;

/// <summary>
/// Thông tin gói dịch vụ (hiển thị cho client)
/// </summary>
public class SubscriptionPlanResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "VND";
    public int? DurationDays { get; set; }
    public bool IsActive { get; set; }
}
