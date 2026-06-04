using System;
using System.Collections.Generic;

namespace VCloset.Application.DTOs.Admin.Requests;

// Request body khi giải quyết một báo cáo vi phạm
public class ResolveReportRequest
{
    public string Action { get; set; } = null!; // "hide_post" hoặc "dismiss"
    public string? ResolutionNotes { get; set; }
}

// Request body khi Admin ẩn hoặc hiện một bài đăng
public class PostVisibilityRequest
{
    public bool IsHidden { get; set; }
    public string? Reason { get; set; }
}

// Response hiển thị nhanh trong danh sách các báo cáo vi phạm
public class ReportSummaryResponse
{
    public Guid ReportId { get; set; }
    public Guid PostId { get; set; }
    public string? PostCaption { get; set; }
    public string PostCreatorDisplayName { get; set; } = null!;
    public string ReporterDisplayName { get; set; } = null!;
    public string Reason { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsResolved { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsPostHidden { get; set; } // Trạng thái ẩn/hiện thực tế của bài đăng bị báo cáo
}

// Chi tiết bài đăng phục vụ Admin xem kỹ trước khi ra quyết định ẩn/xử lý
public class PostModerationDetailResponse
{
    public Guid PostId { get; set; }
    public string? Caption { get; set; }
    public string? CanvasSnapshotUrl { get; set; }
    public string CreatorDisplayName { get; set; } = null!;
    public string CreatorEmail { get; set; } = null!;
    public bool IsHidden { get; set; }
    public bool IsPublic { get; set; }
    public DateTime CreatedAt { get; set; }
    public int LikeCount { get; set; }
    public int CommentCount { get; set; }
    
    // Danh sách các báo cáo gửi tới bài đăng này
    public List<PostReportDetailDto> ActiveReports { get; set; } = new();
}

// Chi tiết thông tin một dòng report đính kèm trong bài đăng
public class PostReportDetailDto
{
    public Guid ReportId { get; set; }
    public string ReporterName { get; set; } = null!;
    public string Reason { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsResolved { get; set; }
    public string? ResolvedByDisplayName { get; set; }
    public DateTime? ResolvedAt { get; set; }
}

// Định dạng trả về có phân trang cho danh sách Báo cáo
public class PagedReportsResponse
{
    public List<ReportSummaryResponse> Reports { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
