using Microsoft.AspNetCore.Mvc;
using VCloset.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading.Tasks;

namespace VCloset.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WardrobeController : ControllerBase
{
    private readonly IBackgroundRemovalService _bgRemovalService;

    public WardrobeController(IBackgroundRemovalService bgRemovalService)
    {
        _bgRemovalService = bgRemovalService;
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
}
