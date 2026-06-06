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
    /// API đối soát và xem lịch sử dòng tiền thanh toán trực tuyến (Momo/PayOS)
    /// </summary>
    [RequirePermission("billing.view")] // standard billing/financial view permission
    [HttpGet("transactions")]
    public async Task<IActionResult> GetPaymentTransactions(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? gateway = null,
        [FromQuery] string? status = null,
        [FromQuery] int? userInternalId = null)
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
                if (Enum.TryParse<VCloset.Domain.Enums.PaymentStatus>(status, true, out var statusEnum))
                {
                    query = query.Where(t => t.Status == statusEnum);
                }
            }

            if (userInternalId.HasValue)
            {
                query = query.Where(t => t.UserInternalId == userInternalId.Value);
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
}
