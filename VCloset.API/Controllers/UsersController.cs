using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using VCloset.Application.DTOs;
using VCloset.Application.Interfaces;

namespace VCloset.API.Controllers;

[Route("api/users")]
[ApiController]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [Authorize]
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
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
}
