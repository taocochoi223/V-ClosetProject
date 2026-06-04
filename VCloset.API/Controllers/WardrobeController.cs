using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VCloset.Application.DTOs;
using VCloset.Application.Interfaces;
using VCloset.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using VCloset.Infrastructure.Data;

namespace VCloset.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WardrobeController : ControllerBase
{
    private readonly IBackgroundRemovalService _bgRemovalService;
    private readonly IWardrobeService _wardrobeService;
    private readonly IStorageService _storageService;
    private readonly VClosetVersion30Context _context;

    public WardrobeController(IBackgroundRemovalService bgRemovalService, IWardrobeService wardrobeService, IStorageService storageService, VClosetVersion30Context context)
    {
        _bgRemovalService = bgRemovalService;
        _wardrobeService = wardrobeService;
        _storageService = storageService;
        _context = context;
    }

    /// <summary>
    /// API loại bỏ phông nền của một hình ảnh (Background Removal).
    /// Nhận file ảnh từ client, gọi Photoroom API để tách nền và trả về file ảnh trong suốt dạng PNG.
    /// </summary>
    [HttpPost("remove-bg")]
    public async Task<IActionResult> RemoveBackground(IFormFile file)
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdString, out int userId)) return Unauthorized();

        if (file == null || file.Length == 0)
            return BadRequest(new { error = "Vui lòng gửi file ảnh (image_file)." });

        // Kiểm tra và trừ lượt AI
        var profile = await _context.CustomerProfiles
            .FirstOrDefaultAsync(cp => cp.UserInternalId == userId);
        if (profile == null)
            return BadRequest(new { error = "Không tìm thấy hồ sơ người dùng." });

        if (profile.BgRemovalCredits <= 0)
            return BadRequest(new { error = "Bạn đã hết lượt tách nền AI của gói miễn phí. Vui lòng nâng cấp Premium để tiếp tục." });

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        var imageBytes = ms.ToArray();

        try
        {
            var resultBytes = await _bgRemovalService.RemoveBackgroundAsync(imageBytes, file.FileName);
            
            // Trừ 1 lượt và lưu
            profile.BgRemovalCredits = Math.Max(0, profile.BgRemovalCredits - 1);
            profile.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

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
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdString, out int userId)) return Unauthorized();

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
            
            var result = await _wardrobeService.CreateItemAsync(userId, dto);

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
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdString, out int userId)) return Unauthorized();
        var items = await _wardrobeService.GetItemsAsync(userId, category, color);
        return Ok(items);
    }

    /// <summary>
    /// API lấy thông tin chi tiết của một món đồ trong tủ đồ qua UUID.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetItem(Guid id)
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdString, out int userId)) return Unauthorized();
        try
        {
            var item = await _wardrobeService.GetItemByIdAsync(userId, id);
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
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdString, out int userId)) return Unauthorized();
        try
        {
            var result = await _wardrobeService.UpdateItemAsync(userId, id, dto);
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
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdString, out int userId)) return Unauthorized();
        try
        {
            await _wardrobeService.DeleteItemAsync(userId, id);
            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
