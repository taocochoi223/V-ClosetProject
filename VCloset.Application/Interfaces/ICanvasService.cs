using VCloset.Application.DTOs;

namespace VCloset.Application.Interfaces;

public interface ICanvasService
{
    Task<OutfitResponseDto> CreateOutfitAsync(int userId, CreateOutfitDto dto, Stream? snapshotStream);
    Task<List<OutfitResponseDto>> GetUserOutfitsAsync(int userId);
    Task<OutfitResponseDto?> GetOutfitByIdAsync(Guid outfitId);
    Task DeleteOutfitAsync(int userId, Guid outfitId);
    Task UpdatePrivacyAsync(int userId, Guid outfitId, bool isPublic);
    Task UpdateTitleAsync(int userId, Guid outfitId, string title);
    /// <summary>Feed cộng đồng: lấy outfit public của mọi người (trừ của mình), sắp xếp mới nhất trước</summary>
    Task<List<CommunityOutfitDto>> GetCommunityOutfitsAsync(int currentUserId, int page, int pageSize);
    /// <summary>Like / Unlike outfit: toggle, trả về số like sau khi thay đổi</summary>
    Task<int> ToggleLikeAsync(int userId, Guid outfitId);
}

