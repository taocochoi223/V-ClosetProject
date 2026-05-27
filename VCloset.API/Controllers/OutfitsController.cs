using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using VCloset.Application.DTOs;
using VCloset.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
namespace VCloset.API.Controllers;

[ApiController]
[Route("api/outfits")]
[Produces("application/json")]
[Authorize]
public class OutfitsController : ControllerBase
{
    private readonly ICanvasService _canvasService;

    public OutfitsController(ICanvasService canvasService)
    {
        _canvasService = canvasService;
    }

    /// <summary>
    /// Tạo một bộ phối đồ mới (2D Canvas)
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(OutfitResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromForm] CreateOutfitDto dto, IFormFile? snapshot)
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdString, out int userId)) return Unauthorized();
        try
        {
            if ((dto.Items == null || dto.Items.Count == 0) && !string.IsNullOrWhiteSpace(dto.ItemsJson))
            {
                dto.Items = JsonSerializer.Deserialize<List<CanvasItemDto>>(
                    dto.ItemsJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                ) ?? new List<CanvasItemDto>();
            }

            using var stream = snapshot?.OpenReadStream();
            var result = await _canvasService.CreateOutfitAsync(userId, dto, stream);

            // Quy chuẩn: POST thành công nên trả về 201 Created kèm theo link lấy Resource đó
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Lấy danh sách bộ đồ của người dùng hiện tại
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<OutfitResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdString, out int userId)) return Unauthorized();
        var result = await _canvasService.GetUserOutfitsAsync(userId);
        return Ok(result);
    }

    /// <summary>
    /// Lấy chi tiết một bộ đồ theo mã định danh (UUID)
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OutfitResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _canvasService.GetOutfitByIdAsync(id);
        if (result == null) return NotFound();
        return Ok(result);
    }

    /// <summary>
    /// Cập nhật trạng thái công khai/riêng tư của bộ đồ
    /// </summary>
    [HttpPatch("{id:guid}/privacy")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdatePrivacy(Guid id, [FromBody] bool isPublic)
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdString, out int userId)) return Unauthorized();
        await _canvasService.UpdatePrivacyAsync(userId, id, isPublic);
        return NoContent(); // Quy chuẩn: Cập nhật thành công không trả về data thì dùng 204
    }

    /// <summary>
    /// Cập nhật tiêu đề bộ đồ
    /// </summary>
    [HttpPut("{id:guid}/title")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateTitle(Guid id, [FromBody] UpdateOutfitTitleDto dto)
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdString, out int userId)) return Unauthorized();
        await _canvasService.UpdateTitleAsync(userId, id, dto.Title);
        return NoContent();
    }

    /// <summary>
    /// Xóa bộ đồ
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdString, out int userId)) return Unauthorized();
        await _canvasService.DeleteOutfitAsync(userId, id);
        return NoContent();
    }
}
