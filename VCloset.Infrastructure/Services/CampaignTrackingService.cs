using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VCloset.Application.Interfaces;
using VCloset.Infrastructure.Data;

namespace VCloset.Infrastructure.Services;

public class CampaignTrackingService : ICampaignTrackingService
{
    private readonly VClosetVersion30Context _context;

    public CampaignTrackingService(VClosetVersion30Context context)
    {
        _context = context;
    }

    public async Task RecordImpressionAsync(Guid campaignId)
    {
        var campaign = await _context.SponsoredCampaigns.FirstOrDefaultAsync(c => c.Id == campaignId);
        
        if (campaign != null && campaign.IsActive)
        {
            campaign.ImpressionCount++;
            _context.SponsoredCampaigns.Update(campaign);
            await _context.SaveChangesAsync();
        }
    }

    public async Task RecordClickAsync(Guid campaignId, decimal costPerClick = 1000m)
    {
        var campaign = await _context.SponsoredCampaigns.FirstOrDefaultAsync(c => c.Id == campaignId);
        
        if (campaign == null || !campaign.IsActive)
            return;

        var brand = await _context.BrandProfiles.FirstOrDefaultAsync(b => b.InternalId == campaign.BrandInternalId);
        
        if (brand == null)
            return;

        // Tăng đếm click
        campaign.ClickCount++;

        // Trừ tiền của Brand và cộng tiền đã tiêu của Campaign
        if (brand.CreditBalance >= costPerClick)
        {
            brand.CreditBalance -= costPerClick;
            campaign.TotalSpent += costPerClick;
        }
        else
        {
            // Trừ vét sạch số tiền còn lại (nếu số dư nhỏ hơn chi phí 1 click)
            campaign.TotalSpent += brand.CreditBalance;
            brand.CreditBalance = 0;
        }

        // Tự động dừng chiến dịch nếu chạm ngưỡng ngân sách ngày HOẶC hết tiền tín dụng
        if (campaign.TotalSpent >= campaign.DailyBudget || brand.CreditBalance <= 0)
        {
            campaign.IsActive = false;
            // Optional: Re-order display rank logic when a campaign stops 
            // có thể cần thiết nếu hệ thống yêu cầu rank phải nhảy lên
            campaign.DisplayRank = 0;
        }

        _context.SponsoredCampaigns.Update(campaign);
        _context.BrandProfiles.Update(brand);
        await _context.SaveChangesAsync();
    }
}
