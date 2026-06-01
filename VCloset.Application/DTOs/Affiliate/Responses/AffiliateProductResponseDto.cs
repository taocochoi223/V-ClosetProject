using System;
using VCloset.Domain.Enums;

namespace VCloset.Application.DTOs.Affiliate.Responses
{
    public class AffiliateProductResponseDto
    {
        public Guid Id { get; set; }
        public string ShopeeProductId { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string ImageUrl { get; set; } = null!;
        public decimal Price { get; set; }
        public string AffiliateLink { get; set; } = null!;
        public ClothingCategory Category { get; set; }
        public bool IsTrending { get; set; }
        public bool IsActive { get; set; }
        public string? Description { get; set; }
    }
}
