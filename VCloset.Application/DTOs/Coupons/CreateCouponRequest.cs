using System;
using System.ComponentModel.DataAnnotations;
using VCloset.Domain.Enums;

namespace VCloset.Application.DTOs.Coupons;

public class CreateCouponRequest
{
    [Required(ErrorMessage = "Mã giảm giá không được để trống")]
    [MaxLength(50)]
    public string Code { get; set; } = null!;

    public DiscountType DiscountType { get; set; } = DiscountType.Percentage;

    [Range(0.01, double.MaxValue, ErrorMessage = "Giá trị giảm phải lớn hơn 0")]
    public decimal DiscountValue { get; set; }

    public int? MaxUses { get; set; }
    
    public DateTime? ExpiresAt { get; set; }
}
