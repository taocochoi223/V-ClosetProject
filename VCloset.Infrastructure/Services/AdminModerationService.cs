using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VCloset.Application.DTOs.Admin.Requests;
using VCloset.Application.Interfaces;
using VCloset.Domain.Entities;
using VCloset.Infrastructure.Data;

namespace VCloset.Infrastructure.Services;

public class AdminModerationService : IAdminModerationService
{
    private readonly VClosetVersion30Context _context;

    public AdminModerationService(VClosetVersion30Context context)
    {
        _context = context;
    }

    // 1. Lấy danh sách báo cáo vi phạm có phân trang và lọc
    public async Task<PagedReportsResponse> GetReportsAsync(int page, int pageSize, bool? isResolved, string? reason)
    {
        var query = _context.PostReports.AsQueryable();

        // Lọc theo trạng thái xử lý
        if (isResolved.HasValue)
        {
            query = query.Where(r => r.IsResolved == isResolved.Value);
        }

        // Lọc theo lý do báo cáo
        if (!string.IsNullOrWhiteSpace(reason))
        {
            var lowerReason = reason.ToLowerInvariant();
            query = query.Where(r => r.Reason.ToLowerInvariant().Contains(lowerReason));
        }

        var totalCount = await query.CountAsync();

        // Phân trang và sắp xếp giảm dần theo ngày tạo
        var reportsList = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var summaries = new List<ReportSummaryResponse>();

        foreach (var r in reportsList)
        {
            // Nạp thông tin Post và Người dùng liên quan trực tiếp từ DbContext
            var post = await _context.CommunityPosts.FirstOrDefaultAsync(p => p.InternalId == r.PostInternalId);
            var reporter = await _context.Users.FirstOrDefaultAsync(u => u.InternalId == r.ReporterInternalId);
            var postCreator = post != null ? await _context.Users.FirstOrDefaultAsync(u => u.InternalId == post.UserInternalId) : null;

            summaries.Add(new ReportSummaryResponse
            {
                ReportId = r.Id,
                PostId = post?.Id ?? Guid.Empty,
                PostCaption = post?.Caption,
                PostCreatorDisplayName = postCreator?.DisplayName ?? "Không xác định",
                ReporterDisplayName = reporter?.DisplayName ?? "Không xác định",
                Reason = r.Reason,
                Description = r.Description,
                IsResolved = r.IsResolved,
                CreatedAt = r.CreatedAt
            });
        }

        return new PagedReportsResponse
        {
            Reports = summaries,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    // 2. Lấy thông tin chi tiết bài viết bị báo cáo phục vụ kiểm duyệt
    public async Task<PostModerationDetailResponse?> GetPostDetailForModerationAsync(Guid postId)
    {
        var post = await _context.CommunityPosts.FirstOrDefaultAsync(p => p.Id == postId);
        if (post == null) return null;

        var creator = await _context.Users.FirstOrDefaultAsync(u => u.InternalId == post.UserInternalId);
        
        // Lấy tất cả báo cáo liên kết với bài đăng này
        var allReports = await _context.PostReports
            .Where(r => r.PostInternalId == post.InternalId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
        
        var activeReportsDto = new List<PostReportDetailDto>();
        foreach (var r in allReports)
        {
            var reporter = await _context.Users.FirstOrDefaultAsync(u => u.InternalId == r.ReporterInternalId);
            var resolver = r.ResolvedByInternal.HasValue 
                ? await _context.Users.FirstOrDefaultAsync(u => u.InternalId == r.ResolvedByInternal.Value) 
                : null;

            activeReportsDto.Add(new PostReportDetailDto
            {
                ReportId = r.Id,
                ReporterName = reporter?.DisplayName ?? "Không xác định",
                Reason = r.Reason,
                Description = r.Description,
                CreatedAt = r.CreatedAt,
                IsResolved = r.IsResolved,
                ResolvedByDisplayName = resolver?.DisplayName,
                ResolvedAt = r.ResolvedAt
            });
        }

        // Lấy CanvasOutfit liên quan để lấy link ảnh phối đồ CanvasSnapshotUrl
        var canvasSnapshot = "";
        if (post.OutfitInternalId.HasValue)
        {
            var outfit = await _context.CanvasOutfits.FirstOrDefaultAsync(o => o.InternalId == post.OutfitInternalId.Value);
            canvasSnapshot = outfit?.CanvasSnapshotUrl;
        }

        return new PostModerationDetailResponse
        {
            PostId = post.Id,
            Caption = post.Caption,
            CanvasSnapshotUrl = canvasSnapshot,
            CreatorDisplayName = creator?.DisplayName ?? "Không xác định",
            CreatorEmail = creator?.Email ?? "Không xác định",
            IsHidden = post.IsHidden,
            IsPublic = post.IsPublic,
            CreatedAt = post.CreatedAt,
            LikeCount = post.LikeCount,
            CommentCount = post.CommentCount,
            ActiveReports = activeReportsDto
        };
    }

    // 3. Giải quyết báo cáo vi phạm (ẩn bài viết hoặc bác bỏ báo cáo)
    public async Task ResolveReportAsync(int adminUserId, Guid reportId, ResolveReportRequest request)
    {
        var report = await _context.PostReports.FirstOrDefaultAsync(r => r.Id == reportId);
        if (report == null)
            throw new Exception("Không tìm thấy thông tin báo cáo vi phạm.");

        if (report.IsResolved)
            throw new Exception("Báo cáo vi phạm này đã được xử lý từ trước.");

        var actionLower = request.Action.ToLowerInvariant();
        if (actionLower != "hide_post" && actionLower != "dismiss")
            throw new Exception("Hành động xử lý (Action) không hợp lệ. Chỉ chấp nhận 'hide_post' hoặc 'dismiss'.");

        // Nếu admin chọn ẩn bài viết vi phạm
        if (actionLower == "hide_post")
        {
            var post = await _context.CommunityPosts.FirstOrDefaultAsync(p => p.InternalId == report.PostInternalId);
            if (post != null)
            {
                post.IsHidden = true;
                post.UpdatedAt = DateTime.UtcNow;
                _context.CommunityPosts.Update(post);
            }
        }

        // Đánh dấu đã giải quyết báo cáo
        report.IsResolved = true;
        report.ResolvedByInternal = adminUserId;
        report.ResolvedAt = DateTime.UtcNow;
        report.Description = string.IsNullOrEmpty(request.ResolutionNotes) 
            ? report.Description 
            : $"{report.Description} | [Ghi chú Admin]: {request.ResolutionNotes}";

        _context.PostReports.Update(report);
        await _context.SaveChangesAsync();
    }

    // 4. Admin chủ động thay đổi ẩn/hiện bài đăng
    public async Task SetPostVisibilityAsync(int adminUserId, Guid postId, PostVisibilityRequest request)
    {
        var post = await _context.CommunityPosts.FirstOrDefaultAsync(p => p.Id == postId);
        if (post == null)
            throw new Exception("Không tìm thấy bài viết yêu cầu.");

        if (post.IsHidden == request.IsHidden)
            throw new Exception($"Trạng thái IsHidden của bài viết đã là {request.IsHidden} rồi.");

        // Cập nhật trạng thái hiển thị
        post.IsHidden = request.IsHidden;
        post.UpdatedAt = DateTime.UtcNow;
        _context.CommunityPosts.Update(post);

        // Đồng thời tự động đánh dấu ĐÃ GIẢI QUYẾT toàn bộ các báo cáo vi phạm chưa xử lý liên quan đến bài viết này
        var pendingReports = await _context.PostReports
            .Where(r => r.PostInternalId == post.InternalId && !r.IsResolved)
            .ToListAsync();

        foreach (var r in pendingReports)
        {
            r.IsResolved = true;
            r.ResolvedByInternal = adminUserId;
            r.ResolvedAt = DateTime.UtcNow;
            r.Description = $"{r.Description} | [Xử lý nhanh của Admin]: Thay đổi trạng thái hiển thị thành IsHidden = {request.IsHidden}. Lý do: {request.Reason ?? "Không có"}";
            _context.PostReports.Update(r);
        }

        await _context.SaveChangesAsync();
    }
}
