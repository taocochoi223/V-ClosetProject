using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VCloset.Application.DTOs;
using VCloset.Application.Interfaces;
using VCloset.Domain.Entities;
using VCloset.Infrastructure.Data;

namespace VCloset.Infrastructure.Services;

public class ClosetService : IClosetService
{
    private readonly VClosetVersion30Context _context;

    public ClosetService(VClosetVersion30Context context)
    {
        _context = context;
    }

    public async Task<List<ClosetDto>> GetClosetsAsync(int userInternalId)
    {
        var closets = await _context.Closets
            .Where(c => c.UserInternalId == userInternalId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        var result = new List<ClosetDto>();

        foreach (var closet in closets)
        {
            // Lấy tất cả items thuộc closet này
            var items = await _context.WardrobeItems
                .Where(w => w.UserInternalId == userInternalId && w.ClosetInternalId == closet.InternalId && w.IsActive)
                .OrderByDescending(w => w.CreatedAt)
                .ToListAsync();

            // Lấy danh sách tối đa 3 ảnh làm thumbnail
            var thumbnailUrls = items
                .Select(w => w.RemovedBgUrl ?? w.OriginalImageUrl)
                .Where(url => !string.IsNullOrEmpty(url))
                .Take(3)
                .ToList();

            result.Add(new ClosetDto
            {
                Id = closet.Id,
                Name = closet.Name,
                ItemCount = items.Count,
                ThumbnailUrls = thumbnailUrls,
                CreatedAt = closet.CreatedAt
            });
        }

        return result;
    }

    public async Task<ClosetDto> CreateClosetAsync(int userInternalId, CreateClosetRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            throw new ArgumentException("Tên tủ đồ không được để trống.");
        }

        // Check if closet name already exists for this user
        var nameExists = await _context.Closets
            .AnyAsync(c => c.UserInternalId == userInternalId && c.Name.ToLower() == dto.Name.Trim().ToLower());
        if (nameExists)
        {
            throw new InvalidOperationException("Bạn đã có một tủ đồ tên này rồi.");
        }

        var closet = new Closet
        {
            UserInternalId = userInternalId,
            Name = dto.Name.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Closets.Add(closet);
        await _context.SaveChangesAsync();

        return new ClosetDto
        {
            Id = closet.Id,
            Name = closet.Name,
            ItemCount = 0,
            ThumbnailUrls = new List<string>(),
            CreatedAt = closet.CreatedAt
        };
    }
}
