using System;
using VCloset.Domain.Enums;

namespace VCloset.Domain.Entities;

/// <summary>
/// Bảng ghi nhận Mã giảm giá (Coupon)
/// </summary>
public class Coupon
{
    public int InternalId { get; set; }
    
    public Guid Id { get; set; }

    /// <summary>
    /// Mã giảm giá (VD: WELCOME, SUMMER50)
    /// </summary>
    public string Code { get; set; } = null!;

    public DiscountType DiscountType { get; set; }

    /// <summary>
    /// Giá trị giảm. (Ví dụ: 20 nếu DiscountType = Percentage, 50000 nếu DiscountType = FixedAmount)
    /// </summary>
    public decimal DiscountValue { get; set; }

    /// <summary>
    /// Số lượt đã được sử dụng thành công
    /// </summary>
    public int CurrentUses { get; set; } = 0;

    /// <summary>
    /// Giới hạn số lượt sử dụng (Null = Không giới hạn)
    /// </summary>
    public int? MaxUses { get; set; }

    /// <summary>
    /// Ngày hết hạn của mã giảm giá (Null = Không bao giờ hết hạn)
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Trạng thái mã giảm giá có đang được phép dùng không
    /// </summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
