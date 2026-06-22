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
        var query = from b in _context.BrandProfiles
                    join u in _context.Users on b.UserInternalId equals u.InternalId into userJoin
                    from u in userJoin.DefaultIfEmpty()
                    select new { Brand = b, User = u };

        // Lọc theo trạng thái Brand
        if (status.HasValue)
        {
            query = query.Where(x => x.Brand.Status == status.Value);
        }

        // Tìm kiếm theo tên thương hiệu hoặc mã số thuế
        if (!string.IsNullOrWhiteSpace(search))
        {
            var lowerSearch = search.ToLowerInvariant();
            query = query.Where(x =>
                x.Brand.BrandName.ToLowerInvariant().Contains(lowerSearch) ||
                (x.Brand.TaxCode != null && x.Brand.TaxCode.ToLowerInvariant().Contains(lowerSearch)));
        }

        var results = await query
            .OrderByDescending(x => x.Brand.CreatedAt)
            .ToListAsync();

        var summaries = new List<BrandSummaryResponse>();

        foreach (var r in results)
        {
            if (r.User == null) continue;

            summaries.Add(new BrandSummaryResponse
            {
                BrandId = r.Brand.Id,
                BrandName = r.Brand.BrandName,
                LogoUrl = r.Brand.LogoUrl,
                WebsiteUrl = r.Brand.WebsiteUrl,
                ContactPhone = r.Brand.ContactPhone,
                TaxCode = r.Brand.TaxCode,
                CreditBalance = r.Brand.CreditBalance,
                Status = r.Brand.Status,
                CreatedAt = r.Brand.CreatedAt,
                UserId = r.User.Id,
                UserEmail = r.User.Email,
                UserDisplayName = r.User.DisplayName
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

        // Khi dừng, các chiến dịch đang chạy phía dưới sẽ được đẩy lên 1 hạng
        var campaignsToShift = await _context.SponsoredCampaigns
            .Where(c => c.Id != campaignId && c.IsActive && c.DisplayRank > campaign.DisplayRank)
            .ToListAsync();

        foreach (var c in campaignsToShift)
        {
            c.DisplayRank--;
            _context.SponsoredCampaigns.Update(c);
        }

        campaign.IsActive = false;
        campaign.DisplayRank = 0; // Đưa về 0 để không chiếm thứ hạng
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

        // Khi chạy lại, tự động xếp vào vị trí cuối cùng
        var maxRank = await _context.SponsoredCampaigns
            .Where(c => c.IsActive)
            .MaxAsync(c => (short?)c.DisplayRank) ?? 0;

        campaign.DisplayRank = (short)(maxRank + 1);
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

        if (request.DailyBudget < 20000)
            throw new Exception("Ngân sách hàng ngày tối thiểu là 20,000 đ để đảm bảo hiệu suất quảng cáo.");
            
        var roundedBudget = request.DailyBudget;

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

        if (campaign.IsActive)
        {
            // Nếu đang chạy mà bị xóa, cần đẩy các chiến dịch phía dưới lên
            var campaignsToShift = await _context.SponsoredCampaigns
                .Where(c => c.Id != campaignId && c.IsActive && c.DisplayRank > campaign.DisplayRank)
                .ToListAsync();

            foreach (var c in campaignsToShift)
            {
                c.DisplayRank--;
                _context.SponsoredCampaigns.Update(c);
            }
        }

        // Thay vì xóa cứng khỏi DB, ta chuyển trạng thái hoạt động về false (Inactive) để giữ lại lịch sử số liệu
        campaign.IsActive = false;
        campaign.DisplayRank = 0;
        _context.SponsoredCampaigns.Update(campaign);
        await _context.SaveChangesAsync();
    }

    // 9. Tìm kiếm, phân trang và sắp xếp chiến dịch quảng cáo
    public async Task<PagedCampaignsResponse> SearchCampaignsAsync(string? search, bool? isActive, string? sortBy, int page, int pageSize)
    {
        var query = (from c in _context.SponsoredCampaigns
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
                             }).AsQueryable();

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

        var totalCount = await query.CountAsync();
        var pagedList = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

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



        if (request.DisplayRank <= 0)
            throw new Exception("Thứ tự hiển thị phải lớn hơn 0.");

        if (request.StartAt >= request.EndAt)
            throw new Exception("Thời gian bắt đầu phải trước thời gian kết thúc.");

        if (request.StartAt < DateTime.UtcNow.AddMinutes(-5))
            throw new Exception("Thời gian bắt đầu chiến dịch không được nằm trong quá khứ.");

        if (request.EndAt <= DateTime.UtcNow)
            throw new Exception("Thời gian kết thúc chiến dịch phải nằm ở tương lai.");

        if (request.DailyBudget < 20000)
            throw new Exception("Ngân sách hàng ngày tối thiểu là 20,000 đ để đảm bảo hiệu suất quảng cáo.");
            
        var roundedBudget = request.DailyBudget;

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
