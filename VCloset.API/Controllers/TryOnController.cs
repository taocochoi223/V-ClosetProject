using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VCloset.Application.Interfaces;
using VCloset.Infrastructure.Data;

namespace VCloset.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TryOnController : ControllerBase
{
    private readonly IVirtualTryOnService _tryOnService;
    private readonly IStorageService _storageService;
    private readonly IWardrobeService _wardrobeService;
    private readonly VClosetVersion30Context _context;

    public TryOnController(
        IVirtualTryOnService tryOnService,
        IStorageService storageService,
        IWardrobeService wardrobeService,
        VClosetVersion30Context context)
    {
        _tryOnService = tryOnService;
        _storageService = storageService;
        _wardrobeService = wardrobeService;
        _context = context;
    }

    /// <summary>
    /// Chạy thử đồ ảo bằng cách truyền trực tiếp URLs của hình ảnh.
    /// </summary>
    [HttpPost("run")]
    public async Task<IActionResult> RunTryOn([FromBody] DirectTryOnRequest request)
    {
        if (request == null)
            return BadRequest(new { error = "Yêu cầu không hợp lệ." });

        if (string.IsNullOrEmpty(request.ModelImageUrl) || string.IsNullOrEmpty(request.GarmentImageUrl))
            return BadRequest(new { error = "Vui lòng cung cấp đầy đủ ModelImageUrl và GarmentImageUrl." });

        try
        {
            var predictionId = await _tryOnService.RunTryOnAsync(
                request.ModelImageUrl,
                request.GarmentImageUrl,
                request.Category,
                request.RestoreBackground
            );
            return Ok(new { predictionId });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Chạy thử đồ ảo bằng cách upload file trực tiếp từ Client.
    /// </summary>
    [HttpPost("run-files")]
    public async Task<IActionResult> RunTryOnWithFiles(
        IFormFile? modelFile,
        IFormFile garmentFile,
        [FromForm] string category = "auto",
        [FromForm] bool restoreBackground = true)
    {
        if (garmentFile == null || garmentFile.Length == 0)
            return BadRequest(new { error = "Vui lòng upload file ảnh quần áo (garmentFile)." });

        try
        {
            // 1. Upload ảnh sản phẩm lên S3 (temp-tryon folder)
            using var garmentStream = garmentFile.OpenReadStream();
            var garmentUrl = await _storageService.UploadFileAsync(garmentStream, garmentFile.FileName, garmentFile.ContentType, "temp-tryon");

            // 2. Xác định ảnh người mẫu
            string modelUrl;
            if (modelFile != null && modelFile.Length > 0)
            {
                using var modelStream = modelFile.OpenReadStream();
                modelUrl = await _storageService.UploadFileAsync(modelStream, modelFile.FileName, modelFile.ContentType, "temp-tryon");
            }
            else
            {
                // Sử dụng mannequin mặc định của tài khoản
                int mockUserId = 1;
                var profile = await _context.CustomerProfiles
                    .FirstOrDefaultAsync(cp => cp.UserInternalId == mockUserId);
                
                if (profile == null || string.IsNullOrEmpty(profile.MannequinImageUrl))
                {
                    return BadRequest(new { error = "Vui lòng cung cấp file ảnh người mẫu hoặc cấu hình ảnh Mannequin trong Profile trước." });
                }
                modelUrl = profile.MannequinImageUrl;
            }

            // 3. Gọi Fashn AI
            var predictionId = await _tryOnService.RunTryOnAsync(modelUrl, garmentUrl, category, restoreBackground);
            return Ok(new { predictionId });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Chạy thử đồ ảo dựa trên một món đồ cụ thể trong tủ đồ (WardrobeItem).
    /// </summary>
    [HttpPost("run-wardrobe")]
    public async Task<IActionResult> RunTryOnWithWardrobe([FromBody] WardrobeTryOnRequest request)
    {
        if (request == null)
            return BadRequest(new { error = "Yêu cầu không hợp lệ." });

        int mockUserId = 1;

        try
        {
            // 1. Lấy thông tin món đồ từ Tủ đồ
            var wardrobeItem = await _wardrobeService.GetItemByIdAsync(mockUserId, request.WardrobeItemId);
            if (wardrobeItem == null || string.IsNullOrEmpty(wardrobeItem.OriginalImageUrl))
            {
                return BadRequest(new { error = "Không tìm thấy món đồ hợp lệ trong tủ đồ." });
            }

            // 2. Xác định ảnh người mẫu
            string modelUrl = request.ModelImageUrl ?? string.Empty;
            if (string.IsNullOrEmpty(modelUrl))
            {
                var profile = await _context.CustomerProfiles
                    .FirstOrDefaultAsync(cp => cp.UserInternalId == mockUserId);
                
                if (profile == null || string.IsNullOrEmpty(profile.MannequinImageUrl))
                {
                    return BadRequest(new { error = "Không tìm thấy ảnh người mẫu. Vui lòng truyền ModelImageUrl hoặc cấu hình ảnh Mannequin trong Profile của bạn." });
                }
                modelUrl = profile.MannequinImageUrl;
            }

            // 3. Gọi Fashn AI
            var predictionId = await _tryOnService.RunTryOnAsync(
                modelUrl,
                wardrobeItem.OriginalImageUrl,
                request.Category,
                request.RestoreBackground
            );
            return Ok(new { predictionId });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Kiểm tra trạng thái của tiến trình thử đồ ảo qua predictionId.
    /// </summary>
    [HttpGet("status/{id}")]
    public async Task<IActionResult> GetStatus(string id)
    {
        if (string.IsNullOrEmpty(id))
            return BadRequest(new { error = "Vui lòng cung cấp predictionId." });

        try
        {
            var (status, outputUrl, error) = await _tryOnService.GetTryOnStatusAsync(id);
            return Ok(new
            {
                status,
                outputUrl,
                error
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

// --- Request DTOs ---

public class DirectTryOnRequest
{
    public string ModelImageUrl { get; set; } = null!;
    public string GarmentImageUrl { get; set; } = null!;
    public string Category { get; set; } = "auto";
    public bool RestoreBackground { get; set; } = true;
}

public class WardrobeTryOnRequest
{
    public Guid WardrobeItemId { get; set; }
    public string? ModelImageUrl { get; set; }
    public string Category { get; set; } = "auto";
    public bool RestoreBackground { get; set; } = true;
}
