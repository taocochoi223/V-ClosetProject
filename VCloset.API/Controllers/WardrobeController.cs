using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VCloset.Application.DTOs;
using VCloset.Application.Interfaces;
using VCloset.Domain.Enums;

namespace VCloset.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WardrobeController : ControllerBase
{
    private readonly IBackgroundRemovalService _bgRemovalService;
    private readonly IWardrobeService _wardrobeService;
    private readonly IStorageService _storageService;

    public WardrobeController(IBackgroundRemovalService bgRemovalService, IWardrobeService wardrobeService, IStorageService storageService)
    {
        _bgRemovalService = bgRemovalService;
        _wardrobeService = wardrobeService;
        _storageService = storageService;
    }

    /// <summary>
    /// API loại bỏ phông nền của một hình ảnh (Background Removal).
    /// Nhận file ảnh từ client, gọi Photoroom API để tách nền và trả về file ảnh trong suốt dạng PNG.
    /// </summary>
    [HttpPost("remove-bg")]
    public async Task<IActionResult> RemoveBackground(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Vui lòng gửi file ảnh (image_file)." });

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        var imageBytes = ms.ToArray();

        try
        {
            var resultBytes = await _bgRemovalService.RemoveBackgroundAsync(imageBytes, file.FileName);
            // Trả về file ảnh PNG trong suốt trực tiếp cho client
            return File(resultBytes, "image/png");
        }
        catch (System.Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// API tải lên hình ảnh và thêm một món đồ mới vào tủ đồ số (Wardrobe Item).
    /// Tự động thực hiện tải ảnh gốc lên Storage (S3/Local) và lưu thông tin chi tiết vào Database.
    /// </summary>
    [HttpPost("upload-and-create")]
    public async Task<IActionResult> UploadAndCreateItem(IFormFile file, [FromForm] ClothingCategory category, [FromForm] string? name, [FromForm] string? brand)
    {
        // Mock UserInternalId = 1 cho đến khi JWT được liên kết hoàn toàn
        int mockUserId = 1; 

        if (file == null || file.Length == 0)
            return BadRequest(new { error = "Vui lòng gửi file ảnh quần áo." });

        try
        {
            // 1. Lưu ảnh qua Storage Service
            using var stream = file.OpenReadStream();
            var imageUrl = await _storageService.UploadFileAsync(stream, file.FileName, file.ContentType);

            // 2. Tạo record trong Database
            var dto = new CreateWardrobeItemDto
            {
                Name = name,
                Category = category,
                OriginalImageUrl = imageUrl,
                Brand = brand
            };
            
            var result = await _wardrobeService.CreateItemAsync(mockUserId, dto);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message }); // Lỗi vượt quá hạn mức freemium (50 món đồ)
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// API lấy danh sách toàn bộ món đồ trong tủ đồ của tôi.
    /// Hỗ trợ lọc theo loại quần áo (ClothingCategory) và màu sắc (Color).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetItems([FromQuery] ClothingCategory? category, [FromQuery] string? color)
    {
        int mockUserId = 1;
        var items = await _wardrobeService.GetItemsAsync(mockUserId, category, color);
        return Ok(items);
    }

    /// <summary>
    /// API lấy thông tin chi tiết của một món đồ trong tủ đồ qua UUID.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetItem(Guid id)
    {
        int mockUserId = 1;
        try
        {
            var item = await _wardrobeService.GetItemByIdAsync(mockUserId, id);
            return Ok(item);
        }
        catch (Exception ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// API cập nhật thông tin (Tên, hãng, ghi chú, thể loại,...) của một món đồ trong tủ đồ qua UUID.
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateItem(Guid id, [FromBody] UpdateWardrobeItemDto dto)
    {
        int mockUserId = 1;
        try
        {
            var result = await _wardrobeService.UpdateItemAsync(mockUserId, id, dto);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// API xóa một món đồ ra khỏi tủ đồ qua UUID.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteItem(Guid id)
    {
        int mockUserId = 1;
        try
        {
            await _wardrobeService.DeleteItemAsync(mockUserId, id);
            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
