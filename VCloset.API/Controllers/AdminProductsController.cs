using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Threading.Tasks;
using VCloset.Application.DTOs.Affiliate.Requests;
using VCloset.Application.DTOs.Affiliate.Responses;
using VCloset.Application.Interfaces;
using VCloset.Infrastructure.Security;

namespace VCloset.API.Controllers
{
    [Route("api/admin/products")]
    [ApiController]
    [Authorize]
    public class AdminProductsController : ControllerBase
    {
        private readonly IAffiliateProductService _productService;
        private readonly IBackgroundRemovalService _bgRemovalService;
        private readonly IStorageService _storageService;

        public AdminProductsController(
            IAffiliateProductService productService,
            IBackgroundRemovalService bgRemovalService,
            IStorageService storageService)
        {
            _productService = productService;
            _bgRemovalService = bgRemovalService;
            _storageService = storageService;
        }

        /// <summary>
        /// API dành cho Admin thêm sản phẩm tiếp thị liên kết Shopee mới
        /// </summary>
        [RequirePermission("product.manage")]
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateProduct([FromBody] CreateAffiliateProductDto dto)
        {
            try
            {
                var result = await _productService.CreateProductAsync(dto);
                return Created(string.Empty, result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Thêm sản phẩm thất bại: {ex.Message}" });
            }
        }

        /// <summary>
        /// API dành cho Admin upload file CSV đối soát đơn hàng từ Shopee Affiliate
        /// </summary>
        [RequirePermission("product.manage")]
        [HttpPost("import-conversions")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> ImportConversions(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Vui lòng tải lên một file CSV hợp lệ.");

            try
            {
                using var stream = file.OpenReadStream();
                int importedCount = await _productService.ImportConversionsAsync(stream);
                return Ok(new { message = $"Đối soát thành công. Đã nhập {importedCount} bản ghi đơn hàng Shopee vào hệ thống." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Đối soát đơn hàng thất bại: {ex.Message}" });
            }
        }
        /// <summary>
        /// API dành cho Admin lấy danh sách sản phẩm tiếp thị (Phân trang, lọc, tìm kiếm)
        /// </summary>
        [RequirePermission("product.manage")]
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetProducts(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] VCloset.Domain.Enums.ClothingCategory? category = null,
            [FromQuery] bool? isActive = null,
            [FromQuery] string? search = null)
        {
            var result = await _productService.GetAdminProductsAsync(page, pageSize, category, isActive, search);
            return Ok(result);
        }

        /// <summary>
        /// API lấy chi tiết sản phẩm tiếp thị dành cho Admin
        /// </summary>
        [RequirePermission("product.manage")]
        [HttpGet("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetProductById(Guid id)
        {
            var result = await _productService.GetProductByIdAsync(id);
            if (result == null) return NotFound("Không tìm thấy sản phẩm yêu cầu.");
            return Ok(result);
        }

        /// <summary>
        /// API dành cho Admin cập nhật thông tin sản phẩm tiếp thị
        /// </summary>
        [RequirePermission("product.manage")]
        [HttpPut("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateProduct(Guid id, [FromBody] UpdateAffiliateProductDto dto)
        {
            try
            {
                var result = await _productService.UpdateProductAsync(id, dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// API dành cho Admin xóa mềm sản phẩm tiếp thị (Chuyển IsActive = false)
        /// </summary>
        [RequirePermission("product.manage")]
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteProduct(Guid id)
        {
            try
            {
                await _productService.DeleteProductAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// API dành cho Admin upload ảnh, thực hiện tách nền, tải lên Cloud Storage và trả về URL ảnh kết quả.
        /// </summary>
        [RequirePermission("product.manage")]
        [HttpPost("remove-bg-upload")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RemoveBackgroundAndUpload(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Vui lòng tải lên một file ảnh." });

            try
            {
                using var ms = new MemoryStream();
                await file.CopyToAsync(ms);
                var imageBytes = ms.ToArray();

                // Gọi dịch vụ tách nền
                var resultBytes = await _bgRemovalService.RemoveBackgroundAsync(imageBytes, file.FileName);

                // Tải lên S3/Local Storage
                using var uploadStream = new MemoryStream(resultBytes);
                var fileName = $"nobg_{Path.GetFileNameWithoutExtension(file.FileName)}.png";
                var imageUrl = await _storageService.UploadFileAsync(uploadStream, fileName, "image/png", "products");

                return Ok(new { imageUrl });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Tách nền và tải ảnh thất bại: {ex.Message}" });
            }
        }
    }
}
