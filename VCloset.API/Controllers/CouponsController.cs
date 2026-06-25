using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VCloset.Application.DTOs.Coupons;
using VCloset.Application.Interfaces;

namespace VCloset.API.Controllers;

[Route("api/subscriptions/coupons")]
[ApiController]
[Authorize] // Requires login to check coupon
public class CouponsController : ControllerBase
{
    private readonly ICouponService _couponService;

    public CouponsController(ICouponService couponService)
    {
        _couponService = couponService;
    }

    [HttpPost("check")]
    public async Task<IActionResult> CheckCoupon([FromBody] CheckCouponRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
            return BadRequest(new { message = "Vui lòng nhập mã giảm giá" });

        var result = await _couponService.CheckCouponAsync(request.Code);
        return Ok(result);
    }
}
