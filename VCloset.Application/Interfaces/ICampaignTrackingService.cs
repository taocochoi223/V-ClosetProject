using System;
using System.Threading.Tasks;

namespace VCloset.Application.Interfaces;

public interface ICampaignTrackingService
{
    /// <summary>
    /// Ghi nhận 1 lượt hiển thị (Impression) của chiến dịch quảng cáo.
    /// Không tính phí đối với Impression.
    /// </summary>
    Task RecordImpressionAsync(Guid campaignId);

    /// <summary>
    /// Ghi nhận 1 lượt nhấp (Click) của chiến dịch quảng cáo.
    /// Sẽ tự động trừ tiền CreditBalance của Brand và cộng TotalSpent của Campaign.
    /// Tự động dừng Campaign nếu hết ngân sách ngày hoặc hết số dư Credit.
    /// </summary>
    Task RecordClickAsync(Guid campaignId, decimal costPerClick = 1000m);
}
