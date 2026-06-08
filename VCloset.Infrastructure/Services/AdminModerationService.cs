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
        var query = from r in _context.PostReports
                    join p in _context.CommunityPosts on r.PostInternalId equals p.InternalId into postJoin
                    from p in postJoin.DefaultIfEmpty()
                    join uReporter in _context.Users on r.ReporterInternalId equals uReporter.InternalId into reporterJoin
                    from uReporter in reporterJoin.DefaultIfEmpty()
                    join uCreator in _context.Users on p.UserInternalId equals uCreator.InternalId into creatorJoin
                    from uCreator in creatorJoin.DefaultIfEmpty()
                    select new { Report = r, Post = p, Reporter = uReporter, Creator = uCreator };

        // Lọc theo trạng thái xử lý
        if (isResolved.HasValue)
        {
            query = query.Where(x => x.Report.IsResolved == isResolved.Value);
        }

        // Lọc theo lý do báo cáo - Dùng ToLower() thay cho ToLowerInvariant() để EF Core dịch sang SQL được
        if (!string.IsNullOrWhiteSpace(reason))
        {
            var lowerReason = reason.ToLower();
            query = query.Where(x => x.Report.Reason.ToLower().Contains(lowerReason));
        }

        var totalCount = await query.CountAsync();

        // Phân trang và sắp xếp giảm dần theo ngày tạo
        var results = await query
            .OrderByDescending(x => x.Report.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var summaries = new List<ReportSummaryResponse>();

        foreach (var x in results)
        {
            summaries.Add(new ReportSummaryResponse
            {
                ReportId = x.Report.Id,
                PostId = x.Post?.Id ?? Guid.Empty,
                PostCaption = x.Post?.Caption,
                PostCreatorDisplayName = x.Creator?.DisplayName ?? "Không xác định",
                ReporterDisplayName = x.Reporter?.DisplayName ?? "Không xác định",
                Reason = x.Report.Reason,
                Description = x.Report.Description,
                IsResolved = x.Report.IsResolved,
                CreatedAt = x.Report.CreatedAt,
                IsPostHidden = x.Post?.IsHidden ?? false // Truyền thông tin ẩn/hiện thực tế của bài đăng về cho Frontend
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
        
        // Lấy tất cả báo cáo liên kết với bài đăng này và nạp thông tin User bằng join
        var allReports = await (from r in _context.PostReports
                                where r.PostInternalId == post.InternalId
                                join u in _context.Users on r.ReporterInternalId equals u.InternalId into rJoin
                                from u in rJoin.DefaultIfEmpty()
                                join res in _context.Users on r.ResolvedByInternal equals res.InternalId into resJoin
                                from res in resJoin.DefaultIfEmpty()
                                orderby r.CreatedAt descending
                                select new { Report = r, Reporter = u, Resolver = res }).ToListAsync();
        
        var activeReportsDto = new List<PostReportDetailDto>();
        foreach (var x in allReports)
        {
            activeReportsDto.Add(new PostReportDetailDto
            {
                ReportId = x.Report.Id,
                ReporterName = x.Reporter?.DisplayName ?? "Không xác định",
                Reason = x.Report.Reason,
                Description = x.Report.Description,
                CreatedAt = x.Report.CreatedAt,
                IsResolved = x.Report.IsResolved,
                ResolvedByDisplayName = x.Resolver?.DisplayName,
                ResolvedAt = x.Report.ResolvedAt
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

                // Đồng thời tự động đánh dấu ĐÃ GIẢI QUYẾT toàn bộ các báo cáo vi phạm CÒN LẠI liên quan đến bài viết này
                var pendingReports = await _context.PostReports
                    .Where(r => r.Id != reportId && r.PostInternalId == post.InternalId && !r.IsResolved)
                    .ToListAsync();

                foreach (var r in pendingReports)
                {
                    r.IsResolved = true;
                    r.ResolvedByInternal = adminUserId;
                    r.ResolvedAt = DateTime.UtcNow;
                    r.Description = $"{r.Description} | [Hệ thống]: Tự động đóng do bài viết đã bị ẩn từ báo cáo khác.";
                    _context.PostReports.Update(r);
                }
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
