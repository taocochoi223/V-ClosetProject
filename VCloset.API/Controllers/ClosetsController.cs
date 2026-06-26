using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VCloset.Application.DTOs;
using VCloset.Application.Interfaces;

namespace VCloset.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ClosetsController : ControllerBase
{
    private readonly IClosetService _closetService;

    public ClosetsController(IClosetService closetService)
    {
        _closetService = closetService;
    }

    /// <summary>
    /// API lấy danh sách tủ đồ của user hiện tại.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetClosets()
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdString, out int userId)) return Unauthorized();

        var result = await _closetService.GetClosetsAsync(userId);
        return Ok(result);
    }

    /// <summary>
    /// API tạo tủ đồ mới.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateCloset([FromBody] CreateClosetRequestDto dto)
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdString, out int userId)) return Unauthorized();

        try
        {
            var result = await _closetService.CreateClosetAsync(userId, dto);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// API đổi tên/cập nhật tủ đồ.
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCloset(Guid id, [FromBody] UpdateClosetRequestDto dto)
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdString, out int userId)) return Unauthorized();

        try
        {
            var result = await _closetService.UpdateClosetAsync(userId, id, dto);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// API xóa tủ đồ.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCloset(Guid id)
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdString, out int userId)) return Unauthorized();

        try
        {
            await _closetService.DeleteClosetAsync(userId, id);
            return Ok(new { message = "Đã xóa tủ đồ thành công." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
