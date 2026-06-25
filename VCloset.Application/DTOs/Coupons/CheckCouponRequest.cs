using System;
using System.ComponentModel.DataAnnotations;

namespace VCloset.Application.DTOs.Coupons;

public class CheckCouponRequest
{
    [Required]
    public string Code { get; set; } = null!;
}

public class CheckCouponResponse
{
    public bool IsValid { get; set; }
    public string? Message { get; set; }
    public string? DiscountType { get; set; }
    public decimal? DiscountValue { get; set; }
}
