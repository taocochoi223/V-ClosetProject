using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using VCloset.Application.DTOs.Chat.Requests;
using VCloset.Application.Interfaces;

namespace VCloset.API.Controllers;

[ApiController]
[Route("api/chat")]
[Produces("application/json")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;

    public ChatController(IChatService chatService)
    {
        _chatService = chatService;
    }

    /// <summary>
    /// API tạm thời lấy danh sách GUID của tất cả người dùng (Dành cho việc test)
    /// </summary>
    [HttpGet("debug-users")]
    [AllowAnonymous]
    public async Task<IActionResult> GetDebugUsers([FromServices] IUnitOfWork unitOfWork)
    {
        var users = await unitOfWork.Users.FindAllAsync(u => u.IsActive);
        var result = users.Select(u => new { u.InternalId, u.DisplayName, GuidId = u.Id });
        return Ok(result);
    }

    /// <summary>
    /// Bắt đầu cuộc trò chuyện 1-1 với một người dùng khác
    /// </summary>
    [HttpPost("rooms/direct")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateDirectRoom([FromBody] CreateDirectRoomRequest request)
    {
        try
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out int userId)) return Unauthorized();

            var result = await _chatService.CreateDirectRoomAsync(userId, request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Tạo phòng chat nhóm mới
    /// </summary>
    [HttpPost("rooms/group")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateGroupRoom([FromBody] CreateGroupRoomRequest request)
    {
        try
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out int userId)) return Unauthorized();

            var result = await _chatService.CreateGroupRoomAsync(userId, request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Lấy danh sách hộp thư phòng chat của người dùng hiện tại
    /// </summary>
    [HttpGet("rooms")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetChatRooms()
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdString, out int userId)) return Unauthorized();

        var result = await _chatService.GetChatRoomsAsync(userId);
        return Ok(result);
    }

    /// <summary>
    /// Lấy lịch sử tin nhắn phòng chat phân trang (được sắp xếp từ mới đến cũ)
    /// </summary>
    [HttpGet("rooms/{roomId:guid}/messages")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetRoomMessages(Guid roomId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out int userId)) return Unauthorized();

            var result = await _chatService.GetRoomMessagesAsync(userId, roomId, page, pageSize);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Gửi tin nhắn văn bản (chữ) vào phòng chat
    /// </summary>
    [HttpPost("rooms/{roomId:guid}/messages/text")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SendTextMessage(Guid roomId, [FromBody] SendTextMessageRequest request)
    {
        try
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out int userId)) return Unauthorized();

            var result = await _chatService.SendTextMessageAsync(userId, roomId, request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Gửi tin nhắn hình ảnh đính kèm (upload lên Supabase/S3 Storage)
    /// </summary>
    [HttpPost("rooms/{roomId:guid}/messages/image")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SendImageMessage(Guid roomId, IFormFile imageFile)
    {
        try
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out int userId)) return Unauthorized();

            var result = await _chatService.SendImageMessageAsync(userId, roomId, imageFile);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Chia sẻ một bộ đồ phối Canvas (Outfit) vào phòng chat
    /// </summary>
    [HttpPost("rooms/{roomId:guid}/messages/outfit")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SendOutfitMessage(Guid roomId, [FromBody] SendOutfitMessageRequest request)
    {
        try
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out int userId)) return Unauthorized();

            var result = await _chatService.SendOutfitMessageAsync(userId, roomId, request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Đánh dấu đã đọc tất cả tin nhắn trong phòng chat
    /// </summary>
    [HttpPut("rooms/{roomId:guid}/read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> MarkMessagesAsRead(Guid roomId)
    {
        try
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out int userId)) return Unauthorized();

            var success = await _chatService.MarkMessagesAsReadAsync(userId, roomId);
            if (!success) return BadRequest(new { message = "Không thể đánh dấu đã đọc. Vui lòng kiểm tra lại phòng chat." });

            return Ok(new { message = "Đã đánh dấu đọc tin nhắn thành công." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Thu hồi tin nhắn
    /// </summary>
    [HttpDelete("messages/{messageId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RecallMessage(Guid messageId)
    {
        try
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out int userId)) return Unauthorized();

            var success = await _chatService.RecallMessageAsync(userId, messageId);
            if (!success) return BadRequest(new { message = "Không thể thu hồi tin nhắn này. Tin nhắn có thể không tồn tại, hoặc bạn không có quyền thu hồi." });

            return Ok(new { message = "Đã thu hồi tin nhắn thành công." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Rời khỏi nhóm chat
    /// </summary>
    [HttpDelete("rooms/{roomId:guid}/leave")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> LeaveGroupRoom(Guid roomId)
    {
        try
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out int userId)) return Unauthorized();

            var success = await _chatService.LeaveGroupRoomAsync(userId, roomId);
            if (!success) return BadRequest(new { message = "Không thể rời khỏi nhóm chat. Vui lòng kiểm tra lại." });

            return Ok(new { message = "Bạn đã rời khỏi nhóm chat thành công." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
