using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using VCloset.Application.DTOs.Admin.Responses;
using VCloset.Application.Interfaces;
using VCloset.Domain.Entities;
using VCloset.Domain.Enums;
using VCloset.Infrastructure.Security;

namespace VCloset.API.Controllers;

[Route("api/admin/payments")]
[ApiController]
[Authorize]
public class AdminPaymentsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public AdminPaymentsController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// API đối soát và xem lịch sử dòng tiền thanh toán trực tuyến (Momo/PayOS).
    /// Hỗ trợ lọc theo cổng, trạng thái, userId, và tìm kiếm tên/email.
    /// </summary>
    [RequirePermission("billing.view")]
    [HttpGet("transactions")]
    public async Task<IActionResult> GetPaymentTransactions(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? gateway = null,
        [FromQuery] string? status = null,
        [FromQuery] int? userInternalId = null,
        [FromQuery] string? searchTerm = null)
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var query = _unitOfWork.PaymentTransactions.Query()
                .Include(t => t.UserInternal)
                .Include(t => t.SubscriptionPlan)
                .AsQueryable();

            if (!string.IsNullOrEmpty(gateway))
            {
                query = query.Where(t => t.PaymentGateway.ToLower() == gateway.ToLower());
            }

            if (!string.IsNullOrEmpty(status))
            {
                if (Enum.TryParse<PaymentStatus>(status, true, out var statusEnum))
                {
                    query = query.Where(t => t.Status == statusEnum);
                }
            }

            if (userInternalId.HasValue)
            {
                query = query.Where(t => t.UserInternalId == userInternalId.Value);
            }

            // Tìm kiếm theo tên hiển thị hoặc email của người dùng
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                query = query.Where(t =>
                    (t.UserInternal.DisplayName != null && t.UserInternal.DisplayName.ToLower().Contains(term)) ||
                    (t.UserInternal.Email != null && t.UserInternal.Email.ToLower().Contains(term)));
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(t => t.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new AdminPaymentTransactionResponse
                {
                    Id = t.Id,
                    UserInternalId = t.UserInternalId,
                    UserDisplayName = t.UserInternal.DisplayName,
                    UserEmail = t.UserInternal.Email,
                    SubscriptionPlanName = t.SubscriptionPlan != null ? t.SubscriptionPlan.Name : "N/A",
                    Amount = t.Amount,
                    Currency = t.Currency,
                    PaymentGateway = t.PaymentGateway,
                    Status = t.Status.ToString(),
                    GatewayTransactionId = t.GatewayTransactionId,
                    CreatedAt = t.CreatedAt,
                    UpdatedAt = t.UpdatedAt
                })
                .ToListAsync();

            return Ok(new PagedPaymentTransactionResponse
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Thống kê doanh thu tổng hợp: tổng tiền, số giao dịch, phân tích theo cổng,
    /// và doanh thu theo từng ngày trong khoảng thời gian chỉ định.
    /// </summary>
    [RequirePermission("billing.view")]
    [HttpGet("revenue-stats")]
    public async Task<IActionResult> GetRevenueStats(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] string? gateway = null)
    {
        try
        {
            var query = _unitOfWork.PaymentTransactions.Query()
                .Include(t => t.UserInternal)
                .AsQueryable();

            // Lọc theo khoảng ngày — chuyển về UTC để tránh lỗi PostgreSQL Kind=Unspecified
            if (fromDate.HasValue)
            {
                var from = DateTime.SpecifyKind(fromDate.Value.Date, DateTimeKind.Utc);
                query = query.Where(t => t.CreatedAt >= from);
            }
            if (toDate.HasValue)
            {
                var to = DateTime.SpecifyKind(toDate.Value.Date.AddDays(1), DateTimeKind.Utc); // bao gồm cả ngày kết thúc
                query = query.Where(t => t.CreatedAt < to);
            }

            // Lọc theo cổng
            if (!string.IsNullOrEmpty(gateway))
            {
                query = query.Where(t => t.PaymentGateway.ToLower() == gateway.ToLower());
            }

            var allTransactions = await query.ToListAsync();

            // Tổng quan
            var totalTransactions = allTransactions.Count;
            var paidStatuses = new[] { PaymentStatus.Success };
            var pendingStatuses = new[] { PaymentStatus.Pending };
            var failedStatuses = new[] { PaymentStatus.Failed, PaymentStatus.Cancelled, PaymentStatus.Expired };

            var paidList = allTransactions.Where(t => paidStatuses.Contains(t.Status)).ToList();
            var paidCount = paidList.Count;
            var pendingCount = allTransactions.Count(t => pendingStatuses.Contains(t.Status));
            var failedCount = allTransactions.Count(t => failedStatuses.Contains(t.Status));
            var totalRevenue = paidList.Sum(t => t.Amount);

            // Phân tích theo cổng (chỉ tính giao dịch thành công)
            var byGateway = paidList
                .GroupBy(t => t.PaymentGateway)
                .Select(g => new RevenueByGatewayDto
                {
                    Gateway = g.Key,
                    TotalAmount = g.Sum(t => t.Amount),
                    Count = g.Count()
                })
                .OrderByDescending(g => g.TotalAmount)
                .ToList();

            // Doanh thu theo ngày (chỉ tính giao dịch thành công)
            var dailyRevenue = paidList
                .GroupBy(t => t.CreatedAt.Date)
                .Select(g => new RevenueDailyPointDto
                {
                    Date = g.Key.ToString("yyyy-MM-dd"),
                    TotalAmount = g.Sum(t => t.Amount),
                    Count = g.Count()
                })
                .OrderBy(g => g.Date)
                .ToList();

            // Điền các ngày không có giao dịch bằng 0 (để biểu đồ liên tục)
            if (fromDate.HasValue && toDate.HasValue && dailyRevenue.Any())
            {
                var filledDays = new List<RevenueDailyPointDto>();
                var current = fromDate.Value.Date;
                var end = toDate.Value.Date;
                while (current <= end)
                {
                    var dateStr = current.ToString("yyyy-MM-dd");
                    var existing = dailyRevenue.FirstOrDefault(d => d.Date == dateStr);
                    filledDays.Add(existing ?? new RevenueDailyPointDto { Date = dateStr, TotalAmount = 0, Count = 0 });
                    current = current.AddDays(1);
                }
                dailyRevenue = filledDays;
            }

            return Ok(new RevenueStatsResponse
            {
                TotalRevenue = totalRevenue,
                TotalTransactions = totalTransactions,
                PaidCount = paidCount,
                PendingCount = pendingCount,
                FailedCount = failedCount,
                ByGateway = byGateway,
                DailyRevenue = dailyRevenue,
                Currency = "VND"
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
