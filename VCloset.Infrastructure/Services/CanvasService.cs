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
            dto.Items ??= new List<CanvasItemDto>();
            await NormalizeCanvasItemsAsync(userId, dto.Items);

            string? imageUrl = null;
            if (snapshotStream != null)
            {
                var snapshotFileName = $"outfit_snapshot_{DateTime.UtcNow:yyyyMMdd_HHmmss}.png";
                imageUrl = await _storageService.UploadFileAsync(snapshotStream, snapshotFileName, "image/png");
            }

            var outfit = new CanvasOutfit
            {
                UserInternalId = userId,
                Title = dto.Title,
                CanvasSnapshotUrl = imageUrl,
                IsPublic = dto.IsPublic,
                CreatedAt = DateTime.UtcNow
            };

            _context.CanvasOutfits.Add(outfit);
            await _context.SaveChangesAsync();

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

    private async Task NormalizeCanvasItemsAsync(int userId, List<CanvasItemDto> items)
    {
        if (items.Count == 0) return;

        var wardrobeUuidIds = items
            .Where(i => !i.WardrobeItemInternalId.HasValue && i.WardrobeItemId.HasValue)
            .Select(i => i.WardrobeItemId!.Value)
            .Distinct()
            .ToList();

        Dictionary<Guid, int> wardrobeMapByUuid = new();
        if (wardrobeUuidIds.Count > 0)
        {
            var wardrobePairs = await _context.WardrobeItems
                .Where(w => w.UserInternalId == userId && w.IsActive && wardrobeUuidIds.Contains(w.Id))
                .Select(w => new { w.Id, w.InternalId })
                .ToListAsync();

            wardrobeMapByUuid = wardrobePairs.ToDictionary(x => x.Id, x => x.InternalId);
            if (wardrobeMapByUuid.Count != wardrobeUuidIds.Count)
            {
                throw new InvalidOperationException("Some selected wardrobe items are invalid or inaccessible.");
            }
        }

        var directInternalIds = items
            .Where(i => i.WardrobeItemInternalId.HasValue)
            .Select(i => i.WardrobeItemInternalId!.Value)
            .Distinct()
            .ToList();

        if (directInternalIds.Count > 0)
        {
            var allowedInternalIds = await _context.WardrobeItems
                .Where(w => w.UserInternalId == userId && w.IsActive && directInternalIds.Contains(w.InternalId))
                .Select(w => w.InternalId)
                .ToListAsync();

            if (allowedInternalIds.Count != directInternalIds.Count)
            {
                throw new InvalidOperationException("Some selected wardrobe items are invalid or inaccessible.");
            }
        }

        for (var index = 0; index < items.Count; index++)
        {
            var item = items[index];

            if (!item.WardrobeItemInternalId.HasValue && item.WardrobeItemId.HasValue)
            {
                item.WardrobeItemInternalId = wardrobeMapByUuid[item.WardrobeItemId.Value];
            }

            var hasWardrobeSource = item.WardrobeItemInternalId.HasValue;
            var hasAffiliateSource = item.AffiliateProductInternalId.HasValue;

            if (hasWardrobeSource == hasAffiliateSource)
            {
                throw new InvalidOperationException($"Item at index {index} must have exactly one source.");
            }

            if (item.Scale <= 0)
            {
                item.Scale = 1;
            }
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

    public async Task UpdateTitleAsync(int userId, Guid outfitId, string title)
    {
        var outfit = await _context.CanvasOutfits
            .FirstOrDefaultAsync(o => o.Id == outfitId && o.UserInternalId == userId);
        if (outfit != null)
        {
            outfit.Title = title;
            await _context.SaveChangesAsync();
        }
    }
}
