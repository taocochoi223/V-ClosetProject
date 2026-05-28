using System;

namespace VCloset.Application.DTOs.Affiliate.Responses
{
    public class AffiliateClickResponseDto
    {
        public Guid ClickId { get; set; }
        public string TargetAffiliateLink { get; set; } = null!;
    }
}
