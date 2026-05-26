using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VCloset.Application.DTOs.Admin.Requests;
using VCloset.Application.Interfaces;
using VCloset.Domain.Entities;
using VCloset.Domain.Enums;
using VCloset.Infrastructure.Data;

namespace VCloset.Infrastructure.Services;

public class AdminBrandService : IAdminBrandService
{
    private readonly VClosetVersion30Context _context;

    public AdminBrandService(VClosetVersion30Context context)
    {
        _context = context;
    }

    // 1. Lấy danh sách Brand Partner có lọc và tìm kiếm
    public async Task<List<BrandSummaryResponse>> GetBrandsAsync(BrandStatus? status, string? search)
    {
        var query = _context.BrandProfiles.AsQueryable();

        // Lọc theo trạng thái Brand
        if (status.HasValue)
        {
            query = query.Where(b => b.Status == status.Value);
        }

        // Tìm kiếm theo tên thương hiệu hoặc mã số thuế
        if (!string.IsNullOrWhiteSpace(search))
        {
            var lowerSearch = search.ToLowerInvariant();
            query = query.Where(b =>
                b.BrandName.ToLowerInvariant().Contains(lowerSearch) ||
                (b.TaxCode != null && b.TaxCode.ToLowerInvariant().Contains(lowerSearch)));
        }

        var brandProfiles = await query
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();

        var summaries = new List<BrandSummaryResponse>();

        foreach (var b in brandProfiles)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.InternalId == b.UserInternalId);
            if (user == null) continue;

            summaries.Add(new BrandSummaryResponse
            {
                BrandId = b.Id,
                BrandName = b.BrandName,
                LogoUrl = b.LogoUrl,
                WebsiteUrl = b.WebsiteUrl,
                ContactPhone = b.ContactPhone,
                TaxCode = b.TaxCode,
                CreditBalance = b.CreditBalance,
                Status = b.Status,
                CreatedAt = b.CreatedAt,
                UserId = user.Id,
                UserEmail = user.Email,
                UserDisplayName = user.DisplayName
            });
        }

        return summaries;
    }

    // 2. Cập nhật trạng thái duyệt hoặc đình chỉ Brand
    public async Task UpdateBrandStatusAsync(int adminUserId, Guid brandId, UpdateBrandStatusRequest request)
    {
        var brand = await _context.BrandProfiles.FirstOrDefaultAsync(b => b.Id == brandId);
        if (brand == null)
            throw new Exception("Không tìm thấy hồ sơ thương hiệu yêu cầu.");

        if (brand.Status == request.Status)
            throw new Exception($"Trạng thái của thương hiệu đã là {request.Status} từ trước.");

        // Cập nhật trạng thái
        brand.Status = request.Status;
        brand.UpdatedAt = DateTime.UtcNow;

        // Nếu duyệt thương hiệu thành VERIFIED, tự động đổi Role của User liên kết thành BrandPartner để họ có quyền hạn
        if (request.Status == BrandStatus.Verified)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.InternalId == brand.UserInternalId);
            if (user != null && user.Role != UserRole.BrandPartner)
            {
                user.Role = UserRole.BrandPartner;
                user.UpdatedAt = DateTime.UtcNow;
                _context.Users.Update(user);
            }
        }
        // Nếu đình chỉ SUSPENDED thương hiệu, đưa Role của User liên kết về lại Customer bình thường
        else if (request.Status == BrandStatus.Suspended)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.InternalId == brand.UserInternalId);
            if (user != null && user.Role == UserRole.BrandPartner)
            {
                user.Role = UserRole.Customer;
                user.UpdatedAt = DateTime.UtcNow;
                _context.Users.Update(user);
            }
        }

        _context.BrandProfiles.Update(brand);
        await _context.SaveChangesAsync();
    }

    // 3. Nạp tiền quảng cáo (Credit) cho Brand
    public async Task RechargeBrandCreditAsync(int adminUserId, Guid brandId, RechargeBrandCreditRequest request)
    {
        if (request.Amount <= 0)
            throw new Exception("Số tiền nạp quảng cáo phải lớn hơn 0.");

        var brand = await _context.BrandProfiles.FirstOrDefaultAsync(b => b.Id == brandId);
        if (brand == null)
            throw new Exception("Không tìm thấy hồ sơ thương hiệu yêu cầu.");

        if (brand.Status != BrandStatus.Verified)
            throw new Exception("Chỉ được nạp tiền quảng cáo cho các thương hiệu đã được duyệt (Verified).");

        // Cộng số dư ví tín dụng quảng cáo
        brand.CreditBalance += request.Amount;
        brand.UpdatedAt = DateTime.UtcNow;

        _context.BrandProfiles.Update(brand);
        await _context.SaveChangesAsync();
    }

    // 4. Lấy danh sách toàn bộ chiến dịch quảng cáo
    public async Task<List<CampaignSummaryResponse>> GetCampaignsAsync()
    {
        var campaigns = await _context.SponsoredCampaigns
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        var summaries = new List<CampaignSummaryResponse>();

        foreach (var c in campaigns)
        {
            var brand = await _context.BrandProfiles.FirstOrDefaultAsync(b => b.InternalId == c.BrandInternalId);
            var product = await _context.AffiliateProducts.FirstOrDefaultAsync(p => p.InternalId == c.AffiliateProductInternalId);

            summaries.Add(new CampaignSummaryResponse
            {
                CampaignId = c.Id,
                BrandName = brand?.BrandName ?? "Không xác định",
                ProductName = product?.Name ?? "Không xác định",
                ProductImageUrl = product?.ImageUrl ?? "https://shopee.vn/favicon.ico",
                DisplayRank = c.DisplayRank,
                DailyBudget = c.DailyBudget,
                TotalSpent = c.TotalSpent,
                ImpressionCount = c.ImpressionCount,
                ClickCount = c.ClickCount,
                IsActive = c.IsActive,
                StartAt = c.StartAt,
                EndAt = c.EndAt,
                CreatedAt = c.CreatedAt
            });
        }

        return summaries;
    }

    // 5. Ngừng khẩn cấp chiến dịch quảng cáo vi phạm
    public async Task StopCampaignAsync(int adminUserId, Guid campaignId)
    {
        var campaign = await _context.SponsoredCampaigns.FirstOrDefaultAsync(c => c.Id == campaignId);
        if (campaign == null)
            throw new Exception("Không tìm thấy chiến dịch quảng cáo yêu cầu.");

        if (!campaign.IsActive)
            throw new Exception("Chiến dịch quảng cáo này hiện đang không hoạt động (đã dừng).");

        // Dừng chiến dịch
        campaign.IsActive = false;
        _context.SponsoredCampaigns.Update(campaign);
        await _context.SaveChangesAsync();
    }
}
