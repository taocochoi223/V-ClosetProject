using System;

namespace VCloset.Domain.Enums;

public enum DiscountType
{
    /// <summary>
    /// Giảm theo phần trăm (Ví dụ: Giảm 20%)
    /// </summary>
    Percentage = 1,

    /// <summary>
    /// Giảm theo số tiền cố định (Ví dụ: Giảm 50.000đ)
    /// </summary>
    FixedAmount = 2
}
