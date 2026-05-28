using System;
using System.Collections.Generic;

namespace VCloset.Application.DTOs.Affiliate.Responses
{
    public class PagedAffiliateProductsResponse
    {
        public List<AffiliateProductResponseDto> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }
}
