using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using VCloset.Application.DTOs;
using VCloset.Application.Interfaces;
using VCloset.Infrastructure.Security;

namespace VCloset.API.Controllers;

[Route("api/users")]
[ApiController]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// API lấy thông tin hồ sơ (Profile) của tôi (Yêu cầu đăng nhập).
    /// Trả về thông tin cá nhân và số đo của tài khoản đang đăng nhập.
    /// </summary>
    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile()
    {
        try
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out int userId)) return Unauthorized();

            var profile = await _userService.GetMyProfileAsync(userId);
            if (profile == null) return NotFound("Người dùng không tồn tại");

            return Ok(profile);
        }
        catch (System.Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// API cập nhật thông tin hồ sơ của tôi (Yêu cầu đăng nhập).
    /// </summary>
    [Authorize]
    [HttpPut("me")]
    public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateProfileRequest request)
    {
        try
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out int userId)) return Unauthorized();

            await _userService.UpdateMyProfileAsync(userId, request);
            
            return Ok("Cập nhật hồ sơ thành công!");
        }
        catch (System.Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// API tải lên và cập nhật ảnh đại diện (Avatar) của tôi (Yêu cầu đăng nhập).
    /// </summary>
    [Authorize]
    [HttpPost("me/avatar")]
    public async Task<IActionResult> UpdateAvatar(IFormFile file)
    {
        try
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out int userId)) return Unauthorized();

            var newAvatarUrl = await _userService.UpdateAvatarAsync(userId, file);

            return Ok(new { AvatarUrl = newAvatarUrl, Message = "Cập nhật ảnh đại diện thành công!" });
        }
        catch (System.Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// API lấy thông tin hồ sơ công khai (Public Profile) của một người dùng khác qua UUID.
    /// Trả về thông tin công khai (không gồm thông tin cá nhân nhạy cảm).
    /// </summary>
    [Authorize]
    [HttpGet("{targetUserId:guid}")]
    public async Task<IActionResult> GetPublicProfile(Guid targetUserId)
    {
        try
        {
            var profile = await _userService.GetPublicProfileAsync(targetUserId);

            if (profile == null) return NotFound("Không tìm thấy người dùng này.");

            return Ok(profile);
        }
        catch (System.Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// API theo dõi (Follow) một người dùng khác qua UUID (Yêu cầu đăng nhập).
    /// </summary>
    [Authorize]
    [HttpPost("{targetUserId:guid}/follow")]
    public async Task<IActionResult> FollowUser(Guid targetUserId)
    {
        try
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out int userId)) return Unauthorized();

            var success = await _userService.FollowUserAsync(userId, targetUserId);
            
            if (!success) return BadRequest("Không thể theo dõi người dùng này.");

            return Ok(new { Message = "Đã theo dõi thành công." });
        }
        catch (System.Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// API bỏ theo dõi (Unfollow) một người dùng khác qua UUID (Yêu cầu đăng nhập).
    /// </summary>
    [Authorize]
    [HttpDelete("{targetUserId:guid}/follow")]
    public async Task<IActionResult> UnfollowUser(Guid targetUserId)
    {
        try
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out int userId)) return Unauthorized();

            var success = await _userService.UnfollowUserAsync(userId, targetUserId);
            
            if (!success) return BadRequest("Không thể bỏ theo dõi người dùng này.");

            return Ok(new { Message = "Đã bỏ theo dõi thành công." });
        }
        catch (System.Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// API lấy danh sách những người đang theo dõi tôi (Followers) (Yêu cầu đăng nhập).
    /// </summary>
    [Authorize]
    [HttpGet("me/followers")]
    public async Task<IActionResult> GetMyFollowers()
    {
        try
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out int userId)) return Unauthorized();

            var followers = await _userService.GetMyFollowersAsync(userId);
            return Ok(followers);
        }
        catch (System.Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// API lấy danh sách những người tôi đang theo dõi (Following) (Yêu cầu đăng nhập).
    /// </summary>
    [Authorize]
    [HttpGet("me/following")]
    public async Task<IActionResult> GetMyFollowing()
    {
        try
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out int userId)) return Unauthorized();

            var followings = await _userService.GetMyFollowingAsync(userId);
            return Ok(followings);
        }
        catch (System.Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// API tự vô hiệu hóa / xóa tài khoản của chính mình (Yêu cầu đăng nhập).
    /// </summary>
    [Authorize]
    [HttpDelete("me")]
    public async Task<IActionResult> DeactivateMyAccount()
    {
        try
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out int userId)) return Unauthorized();

            var success = await _userService.DeactivateMyAccountAsync(userId);
            if (!success) return BadRequest("Không thể xóa tài khoản, hoặc tài khoản đã bị vô hiệu hóa.");

            return Ok(new { Message = "Tài khoản của bạn đã được vô hiệu hóa thành công." });
        }
        catch (System.Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}

