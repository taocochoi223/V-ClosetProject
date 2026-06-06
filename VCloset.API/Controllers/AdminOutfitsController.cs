using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using VCloset.Application.DTOs.Admin.Responses;
using VCloset.Application.Interfaces;
using VCloset.Domain.Entities;
using VCloset.Infrastructure.Security;

namespace VCloset.API.Controllers;

[Route("api/admin/outfits")]
[ApiController]
[Authorize]
public class AdminOutfitsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public AdminOutfitsController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// API lấy danh sách toàn bộ bộ phối đồ của người dùng (Phân trang, lọc)
    /// </summary>
    [RequirePermission("user.view")]
    [HttpGet]
    public async Task<IActionResult> GetOutfits(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? userInternalId = null,
        [FromQuery] bool? isPublic = null,
        [FromQuery] string? search = null)
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var query = _unitOfWork.CanvasOutfits.Query()
                .Include(o => o.UserInternal)
                .AsQueryable();

            if (userInternalId.HasValue)
            {
                query = query.Where(o => o.UserInternalId == userInternalId.Value);
            }

            if (isPublic.HasValue)
            {
                query = query.Where(o => o.IsPublic == isPublic.Value);
            }

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(o => o.Title != null && o.Title.Contains(search));
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(o => o.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new AdminOutfitResponse
                {
                    Id = o.Id,
                    UserInternalId = o.UserInternalId,
                    UserDisplayName = o.UserInternal.DisplayName,
                    UserEmail = o.UserInternal.Email,
                    Title = o.Title,
                    CanvasSnapshotUrl = o.CanvasSnapshotUrl,
                    IsPublic = o.IsPublic,
                    LikeCount = o.LikeCount,
                    CreatedAt = o.CreatedAt
                })
                .ToListAsync();

            return Ok(new PagedOutfitResponse
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// API xóa cưỡng chế bộ phối đồ của người dùng
    /// </summary>
    [RequirePermission("user.moderate")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteOutfit(Guid id)
    {
        try
        {
            var outfit = await _unitOfWork.CanvasOutfits.FindAsync(o => o.Id == id);
            if (outfit == null) return NotFound(new { message = "Không tìm thấy bộ phối đồ." });

            _unitOfWork.CanvasOutfits.Delete(outfit);
            await _unitOfWork.SaveChangesAsync();

            return Ok(new { message = "Đã xóa bộ phối đồ thành công." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
