using VCloset.Domain.Enums;

namespace VCloset.Application.DTOs.Affiliate.Requests
{
    public class UpdateAffiliateProductDto
    {
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string ImageUrl { get; set; } = null!;
        public decimal Price { get; set; }
        public decimal? OriginalPrice { get; set; }
        public ClothingCategory Category { get; set; }
        public string AffiliateLink { get; set; } = null!;
        public bool IsTrending { get; set; }
        public bool IsActive { get; set; }
    }
}
