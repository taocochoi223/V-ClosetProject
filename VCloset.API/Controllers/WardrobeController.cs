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
            // Return the transparent image back to the client directly
            return File(resultBytes, "image/png");
        }
        catch (System.Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("upload-and-create")]
    public async Task<IActionResult> UploadAndCreateItem(IFormFile file, [FromForm] ClothingCategory category, [FromForm] string? name, [FromForm] string? brand)
    {
        // Mock UserInternalId = 1 until JWT is implemented
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

            // Note: Việc gọi Photoroom API nên được đẩy vào Background Job (Hangfire/Quartz)
            // Nhưng tạm thời ta đã lưu được URL và trạng thái Pending.

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message }); // Freemium limit error
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetItems([FromQuery] ClothingCategory? category, [FromQuery] string? color)
    {
        int mockUserId = 1;
        var items = await _wardrobeService.GetItemsAsync(mockUserId, category, color);
        return Ok(items);
    }

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
