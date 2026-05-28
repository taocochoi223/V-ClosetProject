using System;

namespace VCloset.Application.DTOs.Affiliate.Requests
{
    public class RecordAffiliateClickDto
    {
        public Guid ProductId { get; set; }
        public Guid? OutfitId { get; set; }
        public string? ClickSource { get; set; }
    }
}
