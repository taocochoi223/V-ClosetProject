using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VCloset.Application.DTOs.Coupons;
using VCloset.Application.Interfaces;
using VCloset.Domain.Entities;
using VCloset.Domain.Enums;

namespace VCloset.Infrastructure.Services;

public class CouponService : ICouponService
{
    private readonly IUnitOfWork _unitOfWork;

    public CouponService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<CouponDto>> GetAllCouponsAsync()
    {
        var coupons = await _unitOfWork.Coupons.GetAllAsync();
        return coupons.OrderByDescending(c => c.CreatedAt).Select(c => MapToDto(c));
    }

    public async Task<CouponDto> CreateCouponAsync(CreateCouponRequest request)
    {
        var existing = await _unitOfWork.Coupons.FindAsync(c => c.Code.ToLower() == request.Code.ToLower());
        if (existing != null)
        {
            throw new Exception("Mã giảm giá đã tồn tại.");
        }

        var coupon = new Coupon
        {
            Id = Guid.NewGuid(),
            Code = request.Code.ToUpper(),
            DiscountType = request.DiscountType,
            DiscountValue = request.DiscountValue,
            MaxUses = request.MaxUses,
            ExpiresAt = request.ExpiresAt,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Coupons.AddAsync(coupon);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(coupon);
    }

    public async Task<CouponDto> UpdateCouponAsync(Guid id, UpdateCouponRequest request)
    {
        var coupon = await _unitOfWork.Coupons.FindAsync(c => c.Id == id);
        if (coupon == null)
            throw new Exception("Không tìm thấy mã giảm giá.");

        // Check if the new code already exists on another coupon
        if (coupon.Code.ToLower() != request.Code.ToLower())
        {
            var existing = await _unitOfWork.Coupons.FindAsync(c => c.Code.ToLower() == request.Code.ToLower());
            if (existing != null)
                throw new Exception("Mã giảm giá đã tồn tại.");
        }

        coupon.Code = request.Code.ToUpper();
        coupon.DiscountType = request.DiscountType;
        coupon.DiscountValue = request.DiscountValue;
        coupon.MaxUses = request.MaxUses;
        coupon.ExpiresAt = request.ExpiresAt;
        coupon.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Coupons.Update(coupon);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(coupon);
    }

    public async Task<bool> DeleteCouponAsync(Guid id)

    {
        var coupon = await _unitOfWork.Coupons.FindAsync(c => c.Id == id);

        if (coupon == null)
            throw new Exception("Không tìm thấy mã giảm giá.");

        // Instead of hard delete, maybe just deactivate, or hard delete if not used. 
        // We'll allow hard delete for now if it doesn't break foreign keys (no FK in PaymentTransaction, just string)
        _unitOfWork.Coupons.Delete(coupon);
        await _unitOfWork.SaveChangesAsync();
        
        return true;
    }

    public async Task<CouponDto> ToggleCouponActiveAsync(Guid id)
    {
        var coupon = await _unitOfWork.Coupons.FindAsync(c => c.Id == id);

        if (coupon == null)
            throw new Exception("Không tìm thấy mã giảm giá.");

        coupon.IsActive = !coupon.IsActive;
        coupon.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Coupons.Update(coupon);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(coupon);
    }

    public async Task<CheckCouponResponse> CheckCouponAsync(string code, int userInternalId)
    {
        if (string.IsNullOrWhiteSpace(code))
            return new CheckCouponResponse { IsValid = false, Message = "Mã không được để trống" };

        var coupon = await _unitOfWork.Coupons.FindAsync(c => c.Code.ToLower() == code.ToLower());
        
        if (coupon == null)
            return new CheckCouponResponse { IsValid = false, Message = "Mã giảm giá không tồn tại" };

        if (!coupon.IsActive)
            return new CheckCouponResponse { IsValid = false, Message = "Mã giảm giá đã bị vô hiệu hóa" };

        if (coupon.ExpiresAt.HasValue && coupon.ExpiresAt.Value < DateTime.UtcNow)
            return new CheckCouponResponse { IsValid = false, Message = "Mã giảm giá đã hết hạn" };

        if (coupon.MaxUses.HasValue && coupon.CurrentUses >= coupon.MaxUses.Value)
            return new CheckCouponResponse { IsValid = false, Message = "Mã giảm giá đã hết lượt sử dụng" };

        var hasUsed = await _unitOfWork.PaymentTransactions.FindAsync(
            t => t.UserInternalId == userInternalId && 
                 t.Status == PaymentStatus.Success && 
                 t.AppliedCouponCode != null &&
                 t.AppliedCouponCode.ToLower() == code.ToLower());
        if (hasUsed != null)
            return new CheckCouponResponse { IsValid = false, Message = "Bạn đã sử dụng mã giảm giá này rồi" };

        return new CheckCouponResponse
        {
            IsValid = true,
            DiscountType = coupon.DiscountType.ToString().ToLower(),
            DiscountValue = coupon.DiscountValue
        };
    }

    private static CouponDto MapToDto(Coupon coupon)
    {
        return new CouponDto
        {
            Id = coupon.Id,
            Code = coupon.Code,
            DiscountType = coupon.DiscountType.ToString().ToLower(),
            DiscountValue = coupon.DiscountValue,
            CurrentUses = coupon.CurrentUses,
            MaxUses = coupon.MaxUses,
            ExpiresAt = coupon.ExpiresAt,
            IsActive = coupon.IsActive,
            CreatedAt = coupon.CreatedAt
        };
    }
}
