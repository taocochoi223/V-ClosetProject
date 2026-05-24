using System;
using System.Threading.Tasks;
using VCloset.Application.DTOs.Admin.Requests;

namespace VCloset.Application.Interfaces;

public interface IAdminModerationService
{
    // 1. Lấy danh sách báo cáo vi phạm có phân trang và lọc theo trạng thái xử lý
    Task<PagedReportsResponse> GetReportsAsync(int page, int pageSize, bool? isResolved, string? reason);

    // 2. Lấy thông tin chi tiết của bài đăng bị báo cáo phục vụ kiểm duyệt
    Task<PostModerationDetailResponse?> GetPostDetailForModerationAsync(Guid postId);

    // 3. Giải quyết báo cáo vi phạm (ẩn bài viết hoặc bác bỏ báo cáo)
    Task ResolveReportAsync(int adminUserId, Guid reportId, ResolveReportRequest request);

    // 4. Admin chủ động thay đổi trạng thái ẩn/hiện bài viết bất kỳ
    Task SetPostVisibilityAsync(int adminUserId, Guid postId, PostVisibilityRequest request);
}
