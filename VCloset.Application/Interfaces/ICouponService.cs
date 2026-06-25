using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VCloset.Application.DTOs.Coupons;

namespace VCloset.Application.Interfaces;

public interface ICouponService
{
    Task<IEnumerable<CouponDto>> GetAllCouponsAsync();
    Task<CouponDto> CreateCouponAsync(CreateCouponRequest request);
    Task<CouponDto> UpdateCouponAsync(Guid id, UpdateCouponRequest request);
    Task<bool> DeleteCouponAsync(Guid id);

    Task<CouponDto> ToggleCouponActiveAsync(Guid id);
    Task<CheckCouponResponse> CheckCouponAsync(string code, int userInternalId);
}
