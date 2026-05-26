using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VCloset.Application.DTOs.Admin.Requests;
using VCloset.Domain.Enums;

namespace VCloset.Application.Interfaces;

public interface IAdminBrandService
{
    // 1. Lấy danh sách Brand Partner có phân trang, lọc và tìm kiếm
    Task<List<BrandSummaryResponse>> GetBrandsAsync(BrandStatus? status, string? search);

    // 2. Cập nhật trạng thái duyệt hoặc đình chỉ Brand
    Task UpdateBrandStatusAsync(int adminUserId, Guid brandId, UpdateBrandStatusRequest request);

    // 3. Nạp tiền quảng cáo (Credit) cho Brand
    Task RechargeBrandCreditAsync(int adminUserId, Guid brandId, RechargeBrandCreditRequest request);

    // 4. Lấy danh sách toàn bộ chiến dịch quảng cáo
    Task<List<CampaignSummaryResponse>> GetCampaignsAsync();

    // 5. Ngừng khẩn cấp chiến dịch quảng cáo vi phạm
    Task StopCampaignAsync(int adminUserId, Guid campaignId);
}
