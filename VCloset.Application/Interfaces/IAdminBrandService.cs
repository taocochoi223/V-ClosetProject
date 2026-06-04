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

    // 6. Khôi phục/kích hoạt lại chiến dịch quảng cáo đã dừng
    Task ResumeCampaignAsync(int adminUserId, Guid campaignId);

    // 7. Điều chỉnh ngân sách ngày và thứ hạng hiển thị của chiến dịch quảng cáo
    Task AdjustCampaignAsync(int adminUserId, Guid campaignId, AdjustCampaignRequest request);

    // 8. Xóa hoặc lưu trữ chiến dịch quảng cáo
    Task DeleteCampaignAsync(int adminUserId, Guid campaignId);

    // 9. Tìm kiếm, phân trang và sắp xếp chiến dịch quảng cáo
    Task<PagedCampaignsResponse> SearchCampaignsAsync(string? search, bool? isActive, string? sortBy, int page, int pageSize);

    // 10. Xuất báo cáo CSV toàn bộ chiến dịch quảng cáo
    Task<byte[]> ExportCampaignsReportAsync();

    // 11. Lấy số liệu thống kê tổng hợp (KPI Cards) của các chiến dịch quảng cáo
    Task<CampaignDashboardMetricsResponse> GetCampaignDashboardMetricsAsync();

    // 12. Tạo chiến dịch quảng cáo mới
    Task CreateCampaignAsync(CreateCampaignRequest request);

    // 13. Lấy danh sách sản phẩm tiếp thị liên kết đang hoạt động
    Task<List<ProductSelectResponse>> GetActiveProductsAsync();
}
