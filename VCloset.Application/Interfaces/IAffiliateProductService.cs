using System;
using System.IO;
using System.Threading.Tasks;
using VCloset.Application.DTOs.Affiliate.Requests;
using VCloset.Application.DTOs.Affiliate.Responses;
using VCloset.Domain.Enums;

namespace VCloset.Application.Interfaces
{
    public interface IAffiliateProductService
    {
        Task<AffiliateProductResponseDto> CreateProductAsync(CreateAffiliateProductDto dto);
        Task<AffiliateClickResponseDto> RecordClickAsync(int userId, RecordAffiliateClickDto dto);
        Task<int> ImportConversionsAsync(Stream csvStream);
        Task<PagedAffiliateProductsResponse> GetAdminProductsAsync(int page, int pageSize, ClothingCategory? category, bool? isActive, string? search);
        Task<PagedAffiliateProductsResponse> GetClientProductsAsync(int page, int pageSize, ClothingCategory? category, string? search);
        Task<AffiliateProductResponseDto?> GetProductByIdAsync(Guid id);
        Task<AffiliateProductResponseDto> UpdateProductAsync(Guid id, UpdateAffiliateProductDto dto);
        Task DeleteProductAsync(Guid id);
    }
}
