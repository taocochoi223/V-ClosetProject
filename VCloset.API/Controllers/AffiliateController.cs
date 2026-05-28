using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using VCloset.Application.DTOs.Affiliate.Requests;
using VCloset.Application.DTOs.Affiliate.Responses;
using VCloset.Application.Interfaces;

namespace VCloset.API.Controllers
{
    [Route("api/affiliate")]
    [ApiController]
    [Authorize]
    public class AffiliateController : ControllerBase
    {
        private readonly IAffiliateProductService _productService;

        public AffiliateController(IAffiliateProductService productService)
        {
            _productService = productService;
        }

        /// <summary>
        /// API ghi nhận lượt click khi người dùng bấm "Mua ngay" trên App.
        /// API trả về link chuyển hướng có kèm SubId để điều hướng người dùng sang Shopee.
        /// </summary>
        [HttpPost("click")]
        [ProducesResponseType(typeof(AffiliateClickResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> RecordClick([FromBody] RecordAffiliateClickDto dto)
        {
            try
            {
                var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdString, out int userId)) return Unauthorized();

                var result = await _productService.RecordClickAsync(userId, dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        /// <summary>
        /// API lấy danh sách các sản phẩm tiếp thị đang hoạt động để người dùng đưa vào Canvas phối đồ
        /// </summary>
        [HttpGet("products")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetProducts(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] VCloset.Domain.Enums.ClothingCategory? category = null,
            [FromQuery] string? search = null)
        {
            var result = await _productService.GetClientProductsAsync(page, pageSize, category, search);
            return Ok(result);
        }

        /// <summary>
        /// API lấy thông tin chi tiết một sản phẩm tiếp thị đang hoạt động
        /// </summary>
        [HttpGet("products/{id:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetProductById(Guid id)
        {
            var result = await _productService.GetProductByIdAsync(id);
            if (result == null || !result.IsActive) return NotFound("Không tìm thấy sản phẩm hoặc sản phẩm đã bị ẩn.");
            return Ok(result);
        }
    }
}
