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
            .OrderBy(c => c.DisplayRank)
            .ThenByDescending(c => c.CreatedAt)
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

    // 6. Khôi phục/kích hoạt lại chiến dịch quảng cáo đã dừng
    public async Task ResumeCampaignAsync(int adminUserId, Guid campaignId)
    {
        var campaign = await _context.SponsoredCampaigns.FirstOrDefaultAsync(c => c.Id == campaignId);
        if (campaign == null)
            throw new Exception("Không tìm thấy chiến dịch quảng cáo yêu cầu.");

        if (campaign.IsActive)
            throw new Exception("Chiến dịch quảng cáo này hiện đang hoạt động.");

        // Kích hoạt lại
        campaign.IsActive = true;
        _context.SponsoredCampaigns.Update(campaign);
        await _context.SaveChangesAsync();
    }

    // 7. Điều chỉnh ngân sách ngày và thứ hạng hiển thị của chiến dịch quảng cáo
    public async Task AdjustCampaignAsync(int adminUserId, Guid campaignId, AdjustCampaignRequest request)
    {
        var campaign = await _context.SponsoredCampaigns.FirstOrDefaultAsync(c => c.Id == campaignId);
        if (campaign == null)
            throw new Exception("Không tìm thấy chiến dịch quảng cáo yêu cầu.");

        // 1. Làm tròn ngân sách ngày đến hàng nghìn gần nhất (làm tròn 3 số cuối về 000)
        var roundedBudget = Math.Round(request.DailyBudget / 1000m, MidpointRounding.AwayFromZero) * 1000m;
        if (roundedBudget <= 0)
            throw new Exception("Ngân sách hàng ngày sau khi làm tròn phải lớn hơn 0.");

        // 2. Dịch chuyển thứ tự theo kiểu Chèn/Cuộn (Insert/Shift)
        short oldRank = campaign.DisplayRank;
        short newRank = request.DisplayRank;

        if (newRank <= 0)
            throw new Exception("Thứ tự hiển thị phải lớn hơn 0.");

        if (oldRank != newRank)
        {
            if (oldRank < newRank)
            {
                // Di chuyển xuống dưới (ví dụ: 1 -> 5)
                // Các chiến dịch ở giữa (từ oldRank + 1 đến newRank) sẽ bị lùi lên 1 bậc (-1)
                var campaignsToShift = await _context.SponsoredCampaigns
                    .Where(c => c.Id != campaignId && c.IsActive && c.DisplayRank > oldRank && c.DisplayRank <= newRank)
                    .ToListAsync();

                foreach (var c in campaignsToShift)
                {
                    c.DisplayRank--;
                    _context.SponsoredCampaigns.Update(c);
                }
            }
            else
            {
                // Di chuyển lên trên (ví dụ: 5 -> 2)
                // Các chiến dịch ở giữa (từ newRank đến oldRank - 1) sẽ bị tiến xuống 1 bậc (+1)
                var campaignsToShift = await _context.SponsoredCampaigns
                    .Where(c => c.Id != campaignId && c.IsActive && c.DisplayRank >= newRank && c.DisplayRank < oldRank)
                    .ToListAsync();

                foreach (var c in campaignsToShift)
                {
                    c.DisplayRank++;
                    _context.SponsoredCampaigns.Update(c);
                }
            }
        }

        campaign.DailyBudget = roundedBudget;
        campaign.DisplayRank = newRank;

        _context.SponsoredCampaigns.Update(campaign);
        await _context.SaveChangesAsync();
    }

    // 8. Xóa chiến dịch quảng cáo (chuyển thành Dừng hoạt động - Soft-delete)
    public async Task DeleteCampaignAsync(int adminUserId, Guid campaignId)
    {
        var campaign = await _context.SponsoredCampaigns.FirstOrDefaultAsync(c => c.Id == campaignId);
        if (campaign == null)
            throw new Exception("Không tìm thấy chiến dịch quảng cáo yêu cầu.");

        // Thay vì xóa cứng khỏi DB, ta chuyển trạng thái hoạt động về false (Inactive) để giữ lại lịch sử số liệu
        campaign.IsActive = false;
        _context.SponsoredCampaigns.Update(campaign);
        await _context.SaveChangesAsync();
    }

    // 9. Tìm kiếm, phân trang và sắp xếp chiến dịch quảng cáo
    public async Task<PagedCampaignsResponse> SearchCampaignsAsync(string? search, bool? isActive, string? sortBy, int page, int pageSize)
    {
        var rawList = await (from c in _context.SponsoredCampaigns
                             join b in _context.BrandProfiles on c.BrandInternalId equals b.InternalId into brandJoin
                             from b in brandJoin.DefaultIfEmpty()
                             join p in _context.AffiliateProducts on c.AffiliateProductInternalId equals p.InternalId into productJoin
                             from p in productJoin.DefaultIfEmpty()
                             select new CampaignSummaryResponse
                             {
                                 CampaignId = c.Id,
                                 BrandName = b != null ? b.BrandName : "Không xác định",
                                 ProductName = p != null ? p.Name : "Không xác định",
                                 ProductImageUrl = p != null ? p.ImageUrl : "https://shopee.vn/favicon.ico",
                                 DisplayRank = c.DisplayRank,
                                 DailyBudget = c.DailyBudget,
                                 TotalSpent = c.TotalSpent,
                                 ImpressionCount = c.ImpressionCount,
                                 ClickCount = c.ClickCount,
                                 IsActive = c.IsActive,
                                 StartAt = c.StartAt,
                                 EndAt = c.EndAt,
                                 CreatedAt = c.CreatedAt
                             }).ToListAsync();

        var query = rawList.AsQueryable();

        if (isActive.HasValue)
        {
            query = query.Where(c => c.IsActive == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var lowerSearch = search.ToLowerInvariant();
            query = query.Where(r => r.ProductName.ToLowerInvariant().Contains(lowerSearch) || 
                                     r.BrandName.ToLowerInvariant().Contains(lowerSearch));
        }

        if (!string.IsNullOrWhiteSpace(sortBy))
        {
            query = sortBy.ToLowerInvariant() switch
            {
                "budget" => query.OrderByDescending(r => r.DailyBudget),
                "budgetasc" => query.OrderBy(r => r.DailyBudget),
                "spent" => query.OrderByDescending(r => r.TotalSpent),
                "spentasc" => query.OrderBy(r => r.TotalSpent),
                "ctr" => query.OrderByDescending(r => r.ImpressionCount > 0 ? (double)r.ClickCount / r.ImpressionCount : 0),
                "rank" => query.OrderBy(r => r.DisplayRank).ThenByDescending(r => r.CreatedAt),
                _ => query.OrderBy(r => r.DisplayRank).ThenByDescending(r => r.CreatedAt)
            };
        }
        else
        {
            query = query.OrderBy(r => r.DisplayRank).ThenByDescending(r => r.CreatedAt);
        }

        var totalCount = query.Count();
        var pagedList = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return new PagedCampaignsResponse
        {
            Campaigns = pagedList,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    // 10. Xuất báo cáo CSV toàn bộ chiến dịch quảng cáo
    public async Task<byte[]> ExportCampaignsReportAsync()
    {
        var campaigns = await GetCampaignsAsync();
        
        var csv = new System.Text.StringBuilder();
        csv.AppendLine("CampaignId,BrandName,ProductName,DisplayRank,DailyBudget,TotalSpent,Impressions,Clicks,CTR(%),Status,StartAt,EndAt,CreatedAt");
        
        foreach (var c in campaigns)
        {
            var status = c.IsActive ? "Active" : "Stopped";
            var ctr = c.Ctr.ToString("0.00");
            csv.AppendLine($"\"{c.CampaignId}\",\"{c.BrandName}\",\"{c.ProductName}\",{c.DisplayRank},{c.DailyBudget},{c.TotalSpent},{c.ImpressionCount},{c.ClickCount},{ctr},\"{status}\",\"{c.StartAt:yyyy-MM-dd HH:mm:ss}\",\"{c.EndAt:yyyy-MM-dd HH:mm:ss}\",\"{c.CreatedAt:yyyy-MM-dd HH:mm:ss}\"");
        }
        
        return System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
    }

    // 11. Lấy số liệu thống kê tổng hợp (KPI Cards) của các chiến dịch quảng cáo tài trợ
    public async Task<CampaignDashboardMetricsResponse> GetCampaignDashboardMetricsAsync()
    {
        var campaigns = await _context.SponsoredCampaigns.ToListAsync();
        
        int activeCount = campaigns.Count(c => c.IsActive);
        int totalCount = campaigns.Count;
        decimal totalDailyBudget = campaigns.Where(c => c.IsActive).Sum(c => c.DailyBudget);
        decimal totalSpent = campaigns.Sum(c => c.TotalSpent);
        int totalImpressions = campaigns.Sum(c => c.ImpressionCount);
        int totalClicks = campaigns.Sum(c => c.ClickCount);
        double overallCtr = totalImpressions > 0 ? Math.Round((double)totalClicks / totalImpressions * 100, 2) : 0.0;

        return new CampaignDashboardMetricsResponse
        {
            ActiveCampaignsCount = activeCount,
            TotalCampaignsCount = totalCount,
            TotalDailyBudget = totalDailyBudget,
            TotalSpent = totalSpent,
            TotalImpressions = totalImpressions,
            TotalClicks = totalClicks,
            OverallCtr = overallCtr
        };
    }

    // 12. Tạo chiến dịch quảng cáo mới
    public async Task CreateCampaignAsync(CreateCampaignRequest request)
    {
        var brand = await _context.BrandProfiles.FirstOrDefaultAsync(b => b.Id == request.BrandId);
        if (brand == null)
            throw new Exception("Không tìm thấy đối tác thương hiệu yêu cầu.");

        // Kiểm tra trạng thái thương hiệu: Chỉ thương hiệu đã VERIFIED mới được tạo quảng cáo
        if (brand.Status != BrandStatus.Verified)
            throw new Exception("Chỉ thương hiệu đã được phê duyệt (Verified) mới được phép tạo chiến dịch.");

        var product = await _context.AffiliateProducts.FirstOrDefaultAsync(p => p.Id == request.ProductId);
        if (product == null)
            throw new Exception("Không tìm thấy sản phẩm liên kết yêu cầu.");

        // Kiểm tra trạng thái sản phẩm: Sản phẩm phải đang hoạt động
        if (!product.IsActive)
            throw new Exception("Sản phẩm tiếp thị liên kết này đang tạm ngưng hoạt động.");

        if (request.DailyBudget <= 0)
            throw new Exception("Ngân sách hàng ngày phải lớn hơn 0.");

        if (request.DisplayRank <= 0)
            throw new Exception("Thứ tự hiển thị phải lớn hơn 0.");

        if (request.StartAt >= request.EndAt)
            throw new Exception("Thời gian bắt đầu phải trước thời gian kết thúc.");

        if (request.EndAt <= DateTime.UtcNow)
            throw new Exception("Thời gian kết thúc chiến dịch phải nằm ở tương lai.");

        // Làm tròn ngân sách ngày đến hàng nghìn gần nhất (làm tròn 3 số cuối về 000)
        var roundedBudget = Math.Round(request.DailyBudget / 1000m, MidpointRounding.AwayFromZero) * 1000m;
        if (roundedBudget <= 0)
            throw new Exception("Ngân sách hàng ngày sau khi làm tròn phải lớn hơn 0.");

        // Xử lý chèn/cuộn thứ tự hiển thị (Insert/Shift) cho vị trí mới
        var newRank = request.DisplayRank;
        var campaignsToShift = await _context.SponsoredCampaigns
            .Where(c => c.IsActive && c.DisplayRank >= newRank)
            .OrderBy(c => c.DisplayRank)
            .ToListAsync();

        short currentShiftRank = (short)(newRank + 1);
        foreach (var c in campaignsToShift)
        {
            if (c.DisplayRank < currentShiftRank)
            {
                c.DisplayRank = currentShiftRank;
                _context.SponsoredCampaigns.Update(c);
            }
            currentShiftRank++;
        }

        var newCampaign = new SponsoredCampaign
        {
            Id = Guid.NewGuid(),
            BrandInternalId = brand.InternalId,
            AffiliateProductInternalId = product.InternalId,
            DisplayRank = newRank,
            DailyBudget = roundedBudget,
            TotalSpent = 0,
            ImpressionCount = 0,
            ClickCount = 0,
            StartAt = request.StartAt,
            EndAt = request.EndAt,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _context.SponsoredCampaigns.AddAsync(newCampaign);
        await _context.SaveChangesAsync();
    }

    // 13. Lấy danh sách sản phẩm tiếp thị liên kết đang hoạt động
    public async Task<List<ProductSelectResponse>> GetActiveProductsAsync()
    {
        return await _context.AffiliateProducts
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .Select(p => new ProductSelectResponse
            {
                ProductId = p.Id,
                ProductName = p.Name,
                ProductImageUrl = p.ImageUrl
            })
            .ToListAsync();
    }
}
