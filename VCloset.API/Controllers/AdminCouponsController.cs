using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VCloset.Infrastructure.Security;
using VCloset.Application.DTOs.Coupons;
using VCloset.Application.Interfaces;



namespace VCloset.API.Controllers;

[Route("api/admin/coupons")]
[ApiController]
[Authorize(Roles = "Admin,Moderator")]
[RequirePermission("coupon.manage")]
public class AdminCouponsController : ControllerBase

{
    private readonly ICouponService _couponService;

    public AdminCouponsController(ICouponService couponService)
    {
        _couponService = couponService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllCoupons()
    {
        var coupons = await _couponService.GetAllCouponsAsync();
        return Ok(coupons);
    }

    [HttpPost]
    public async Task<IActionResult> CreateCoupon([FromBody] CreateCouponRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var result = await _couponService.CreateCouponAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}/toggle")]
    public async Task<IActionResult> ToggleCouponStatus(Guid id)
    {
        try
        {
            var result = await _couponService.ToggleCouponActiveAsync(id);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCoupon(Guid id)
    {
        try
        {
            await _couponService.DeleteCouponAsync(id);
            return Ok(new { message = "Xóa mã giảm giá thành công." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
