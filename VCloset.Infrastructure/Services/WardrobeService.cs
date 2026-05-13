using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VCloset.Application.DTOs;
using VCloset.Application.Interfaces;
using VCloset.Domain.Entities;
using VCloset.Domain.Enums;
using VCloset.Infrastructure.Data;

namespace VCloset.Infrastructure.Services;

public class WardrobeService : IWardrobeService
{
    private readonly VClosetVersion30Context _context;

    public WardrobeService(VClosetVersion30Context context)
    {
        _context = context;
    }

    public async Task<WardrobeItemResponseDto> CreateItemAsync(int userInternalId, CreateWardrobeItemDto dto)
    {
        // Check freemium limit (30 items)
        var customerProfile = await _context.CustomerProfiles
            .FirstOrDefaultAsync(c => c.UserInternalId == userInternalId);

        if (customerProfile != null)
        {
            var hasPremium = await _context.PremiumSubscriptions
                .AnyAsync(s => s.UserInternalId == userInternalId && s.IsActive && s.ExpiresAt > DateTime.UtcNow);

            if (!hasPremium && customerProfile.WardrobeItemCount >= 30)
            {
                throw new InvalidOperationException("Bạn đã đạt giới hạn 30 món đồ của tài khoản miễn phí. Vui lòng nâng cấp Premium.");
            }
        }
        else
        {
            var count = await _context.WardrobeItems.CountAsync(w => w.UserInternalId == userInternalId && w.IsActive);
            if (count >= 30)
            {
                var hasPremium = await _context.PremiumSubscriptions
                    .AnyAsync(s => s.UserInternalId == userInternalId && s.IsActive && s.ExpiresAt > DateTime.UtcNow);
                if (!hasPremium) throw new InvalidOperationException("Bạn đã đạt giới hạn 30 món đồ của tài khoản miễn phí. Vui lòng nâng cấp Premium.");
            }
        }

        var item = new WardrobeItem
        {
            UserInternalId = userInternalId,
            Name = dto.Name,
            OriginalImageUrl = dto.OriginalImageUrl,
            Category = dto.Category,
            ColorTags = dto.ColorTags,
            Brand = dto.Brand,
            Notes = dto.Notes,
            IsActive = true,
            BgRemovalStatus = AiJobStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.WardrobeItems.Add(item);
        await _context.SaveChangesAsync();

        return MapToDto(item);
    }

    public async Task<WardrobeItemResponseDto> GetItemByIdAsync(int userInternalId, Guid itemId)
    {
        var item = await _context.WardrobeItems
            .FirstOrDefaultAsync(w => w.Id == itemId && w.UserInternalId == userInternalId && w.IsActive);

        if (item == null) throw new Exception("Không tìm thấy món đồ.");

        return MapToDto(item);
    }

    public async Task<List<WardrobeItemResponseDto>> GetItemsAsync(int userInternalId, ClothingCategory? category = null, string? color = null)
    {
        var query = _context.WardrobeItems
            .Where(w => w.UserInternalId == userInternalId && w.IsActive);

        if (category.HasValue)
        {
            query = query.Where(w => w.Category == category.Value);
        }

        if (!string.IsNullOrEmpty(color))
        {
            query = query.Where(w => w.ColorTags != null && w.ColorTags.Contains(color));
        }

        var items = await query.OrderByDescending(w => w.CreatedAt).ToListAsync();

        return items.Select(MapToDto).ToList();
    }

    public async Task<WardrobeItemResponseDto> UpdateItemAsync(int userInternalId, Guid itemId, UpdateWardrobeItemDto dto)
    {
        var item = await _context.WardrobeItems
            .FirstOrDefaultAsync(w => w.Id == itemId && w.UserInternalId == userInternalId && w.IsActive);

        if (item == null) throw new Exception("Không tìm thấy món đồ.");

        if (dto.Name != null) item.Name = dto.Name;
        if (dto.Brand != null) item.Brand = dto.Brand;
        if (dto.Notes != null) item.Notes = dto.Notes;
        if (dto.Category.HasValue) item.Category = dto.Category.Value;
        if (dto.ColorTags != null) item.ColorTags = dto.ColorTags;

        item.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapToDto(item);
    }

    public async Task DeleteItemAsync(int userInternalId, Guid itemId)
    {
        var item = await _context.WardrobeItems
            .FirstOrDefaultAsync(w => w.Id == itemId && w.UserInternalId == userInternalId && w.IsActive);

        if (item == null) throw new Exception("Không tìm thấy món đồ.");

        // Soft Delete
        item.IsActive = false;
        item.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    private static WardrobeItemResponseDto MapToDto(WardrobeItem item)
    {
        return new WardrobeItemResponseDto
        {
            Id = item.Id,
            Name = item.Name,
            OriginalImageUrl = item.OriginalImageUrl,
            RemovedBgUrl = item.RemovedBgUrl,
            BgRemovalStatus = item.BgRemovalStatus,
            Category = item.Category.ToString(),
            ColorTags = item.ColorTags ?? new List<string>(),
            Brand = item.Brand,
            Notes = item.Notes,
            CreatedAt = item.CreatedAt
        };
    }
}