using System;

namespace VCloset.Application.DTOs.Subscriptions.Requests;

public class GrantSubscriptionRequest
{
    public int TargetUserId { get; set; }
    public Guid PlanId { get; set; }
    public string? AdminNote { get; set; }
}
