using Microsoft.EntityFrameworkCore;
using VCloset.Application.DTOs;
using VCloset.Application.Interfaces;
using VCloset.Domain.Entities;
using VCloset.Infrastructure.Data;

namespace VCloset.Infrastructure.Services;

public class CanvasService : ICanvasService
{
    private readonly VClosetVersion30Context _context;
    private readonly IStorageService _storageService;

    public CanvasService(VClosetVersion30Context context, IStorageService storageService)
    {
        _context = context;
        _storageService = storageService;
    }

    public async Task<OutfitResponseDto> CreateOutfitAsync(int userId, CreateOutfitDto dto, Stream? snapshotStream)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // 1. Xử lý lưu ảnh snapshot (nếu có)
            string? imageUrl = null;
            if (snapshotStream != null)
            {
                imageUrl = await _storageService.UploadFileAsync(snapshotStream, "outfit_snapshot.png", "image/png");
            }

            // 2. Tạo Outfit gốc
            var outfit = new CanvasOutfit
            {
                UserInternalId = userId,
                Title = dto.Title,
                CanvasSnapshotUrl = imageUrl,
                IsPublic = dto.IsPublic,
                CreatedAt = DateTime.UtcNow
            };

            _context.CanvasOutfits.Add(outfit);
            await _context.SaveChangesAsync(); // Lưu để lấy OutfitInternalId

            // 3. Lưu danh sách các món đồ (Items) trên canvas
            foreach (var itemDto in dto.Items)
            {
                var item = new CanvasOutfitItem
                {
                    OutfitInternalId = outfit.InternalId,
                    WardrobeItemInternalId = itemDto.WardrobeItemInternalId,
                    AffiliateProductInternalId = itemDto.AffiliateProductInternalId,
                    PosX = itemDto.PosX,
                    PosY = itemDto.PosY,
                    Scale = itemDto.Scale,
                    Rotation = itemDto.Rotation,
                    ZIndex = itemDto.ZIndex
                };
                _context.CanvasOutfitItems.Add(item);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return new OutfitResponseDto
            {
                Id = outfit.Id,
                Title = outfit.Title ?? "Untitled",
                CanvasSnapshotUrl = outfit.CanvasSnapshotUrl,
                IsPublic = outfit.IsPublic,
                CreatedAt = outfit.CreatedAt
            };
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<List<OutfitResponseDto>> GetUserOutfitsAsync(int userId)
    {
        return await _context.CanvasOutfits
            .Where(o => o.UserInternalId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new OutfitResponseDto
            {
                Id = o.Id,
                Title = o.Title ?? "Untitled",
                CanvasSnapshotUrl = o.CanvasSnapshotUrl,
                IsPublic = o.IsPublic,
                CreatedAt = o.CreatedAt
            })
            .ToListAsync();
    }

    public async Task UpdatePrivacyAsync(int userId, Guid outfitId, bool isPublic)
    {
        var outfit = await _context.CanvasOutfits
            .FirstOrDefaultAsync(o => o.Id == outfitId && o.UserInternalId == userId);

        if (outfit != null)
        {
            outfit.IsPublic = isPublic;
            await _context.SaveChangesAsync();
        }
    }

    //public Task<OutfitResponseDto?> GetOutfitByIdAsync(Guid outfitId) => throw new NotImplementedException();
    public async Task<OutfitResponseDto?> GetOutfitByIdAsync(Guid outfitId)
    {
        return await _context.CanvasOutfits
            .Where(o => o.Id == outfitId)
            .Select(o => new OutfitResponseDto
            {
                Id = o.Id,
                Title = o.Title ?? "Untitled",
                CanvasSnapshotUrl = o.CanvasSnapshotUrl,
                IsPublic = o.IsPublic,
                CreatedAt = o.CreatedAt
            })
            .FirstOrDefaultAsync();
    }

    //public Task DeleteOutfitAsync(int userId, Guid outfitId) => throw new NotImplementedException();
    public async Task DeleteOutfitAsync(int userId, Guid outfitId)
    {
        var outfit = await _context.CanvasOutfits
            .FirstOrDefaultAsync(o => o.Id == outfitId && o.UserInternalId == userId);
        if (outfit == null) return;
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            if (!string.IsNullOrEmpty(outfit.CanvasSnapshotUrl))
            {
                await _storageService.DeleteFileAsync(outfit.CanvasSnapshotUrl);
            }
            _context.CanvasOutfits.Remove(outfit);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
