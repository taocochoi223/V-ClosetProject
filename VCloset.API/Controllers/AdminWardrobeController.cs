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

[Route("api/admin/wardrobe")]
[ApiController]
[Authorize]
public class AdminWardrobeController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public AdminWardrobeController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// API lấy danh sách toàn bộ ảnh tủ đồ của người dùng (Phân trang, lọc)
    /// </summary>
    [RequirePermission("user.view")]
    [HttpGet]
    public async Task<IActionResult> GetWardrobeItems(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? userInternalId = null,
        [FromQuery] string? category = null,
        [FromQuery] string? search = null)
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var query = _unitOfWork.WardrobeItems.Query()
                .Include(w => w.UserInternal)
                .AsQueryable();

            if (userInternalId.HasValue)
            {
                query = query.Where(w => w.UserInternalId == userInternalId.Value);
            }

            if (!string.IsNullOrEmpty(category))
            {
                if (Enum.TryParse<VCloset.Domain.Enums.ClothingCategory>(category, true, out var catEnum))
                {
                    query = query.Where(w => w.Category == catEnum);
                }
            }

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(w => (w.Name != null && w.Name.Contains(search)) || 
                                         (w.Brand != null && w.Brand.Contains(search)) || 
                                         (w.Notes != null && w.Notes.Contains(search)));
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(w => w.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(w => new AdminWardrobeItemResponse
                {
                    Id = w.Id,
                    UserInternalId = w.UserInternalId,
                    UserDisplayName = w.UserInternal.DisplayName,
                    UserEmail = w.UserInternal.Email,
                    Name = w.Name,
                    OriginalImageUrl = w.OriginalImageUrl,
                    RemovedBgUrl = w.RemovedBgUrl,
                    Brand = w.Brand,
                    Notes = w.Notes,
                    IsActive = w.IsActive,
                    Category = w.Category.ToString(),
                    BgRemovalStatus = w.BgRemovalStatus.ToString(),
                    CreatedAt = w.CreatedAt
                })
                .ToListAsync();

            return Ok(new PagedWardrobeResponse
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
    /// API khóa/mở khóa hình ảnh món đồ trong tủ đồ của user
    /// </summary>
    [RequirePermission("user.moderate")]
    [HttpPut("{id:guid}/deactivate")]
    public async Task<IActionResult> ToggleDeactivate(Guid id, [FromBody] bool isActive)
    {
        try
        {
            var item = await _unitOfWork.WardrobeItems.FindAsync(w => w.Id == id);
            if (item == null) return NotFound(new { message = "Không tìm thấy hình ảnh tủ đồ." });

            item.IsActive = isActive;
            item.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.WardrobeItems.Update(item);
            await _unitOfWork.SaveChangesAsync();

            return Ok(new { message = $"Đã cập nhật trạng thái hình ảnh thành {(isActive ? "Hoạt động" : "Khóa")}." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// API xóa cưỡng chế món đồ khỏi tủ đồ của user (nhạy cảm/vi phạm bản quyền)
    /// </summary>
    [RequirePermission("user.moderate")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteWardrobeItem(Guid id)
    {
        try
        {
            var item = await _unitOfWork.WardrobeItems.FindAsync(w => w.Id == id);
            if (item == null) return NotFound(new { message = "Không tìm thấy hình ảnh tủ đồ." });

            _unitOfWork.WardrobeItems.Delete(item);
            await _unitOfWork.SaveChangesAsync();

            return Ok(new { message = "Đã xóa hình ảnh tủ đồ thành công." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// API xóa hàng loạt hình ảnh món đồ trong tủ đồ (admin)
    /// </summary>
    [RequirePermission("user.moderate")]
    [HttpPost("bulk-delete")]
    public async Task<IActionResult> BulkDeleteWardrobeItems([FromBody] System.Collections.Generic.List<Guid> ids)
    {
        try
        {
            if (ids == null || ids.Count == 0) return BadRequest(new { message = "Danh sách ID không được để trống." });

            var items = await _unitOfWork.WardrobeItems.FindAllAsync(w => ids.Contains(w.Id));
            foreach (var item in items)
            {
                _unitOfWork.WardrobeItems.Delete(item);
            }
            await _unitOfWork.SaveChangesAsync();

            return Ok(new { message = $"Đã xóa hàng loạt {items.Count()} hình ảnh tủ đồ thành công." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
