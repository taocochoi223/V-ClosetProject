using System;

namespace VCloset.Application.DTOs.Coupons;

public class CouponDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public string DiscountType { get; set; } = null!; // "percentage" hoặc "fixed_amount"
    public decimal DiscountValue { get; set; }
    public int CurrentUses { get; set; }
    public int? MaxUses { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
