using System;
using System.Collections.Generic;

namespace VCloset.Application.DTOs.Admin.Responses;

public class AdminWardrobeItemResponse
{
    public Guid Id { get; set; }
    public int UserInternalId { get; set; }
    public string UserDisplayName { get; set; } = null!;
    public string UserEmail { get; set; } = null!;
    public string? Name { get; set; }
    public string OriginalImageUrl { get; set; } = null!;
    public string? RemovedBgUrl { get; set; }
    public string? Brand { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
    public string Category { get; set; } = null!;
    public string BgRemovalStatus { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}

public class PagedWardrobeResponse
{
    public List<AdminWardrobeItemResponse> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

public class AdminOutfitResponse
{
    public Guid Id { get; set; }
    public int UserInternalId { get; set; }
    public string UserDisplayName { get; set; } = null!;
    public string UserEmail { get; set; } = null!;
    public string? Title { get; set; }
    public string? CanvasSnapshotUrl { get; set; }
    public bool IsPublic { get; set; }
    public int LikeCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PagedOutfitResponse
{
    public List<AdminOutfitResponse> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

public class AdminPaymentTransactionResponse
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string UserDisplayName { get; set; } = null!;

    public string UserEmail { get; set; } = null!;
    public string SubscriptionPlanName { get; set; } = null!;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "VND";
    public string PaymentGateway { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string? GatewayTransactionId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class PagedPaymentTransactionResponse
{
    public List<AdminPaymentTransactionResponse> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

public class RevenueByGatewayDto
{
    public string Gateway { get; set; } = null!;
    public decimal TotalAmount { get; set; }
    public int Count { get; set; }
}

public class RevenueDailyPointDto
{
    public string Date { get; set; } = null!; // yyyy-MM-dd
    public decimal TotalAmount { get; set; }
    public int Count { get; set; }
}

public class RevenueStatsResponse
{
    public decimal TotalRevenue { get; set; }
    public int TotalTransactions { get; set; }
    public int PaidCount { get; set; }
    public int PendingCount { get; set; }
    public int FailedCount { get; set; }
    public List<RevenueByGatewayDto> ByGateway { get; set; } = new();
    public List<RevenueDailyPointDto> DailyRevenue { get; set; } = new();
    public string Currency { get; set; } = "VND";
}
