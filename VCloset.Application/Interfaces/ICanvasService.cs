using VCloset.Application.DTOs;

namespace VCloset.Application.Interfaces;

public interface ICanvasService
{
    Task<OutfitResponseDto> CreateOutfitAsync(int userId, CreateOutfitDto dto, Stream? snapshotStream);
    Task<List<OutfitResponseDto>> GetUserOutfitsAsync(int userId);
    Task<OutfitResponseDto?> GetOutfitByIdAsync(Guid outfitId);
    Task DeleteOutfitAsync(int userId, Guid outfitId);
    Task UpdatePrivacyAsync(int userId, Guid outfitId, bool isPublic);
}

