using System.Collections.Generic;
using System.Threading.Tasks;
using VCloset.Application.DTOs.TierConfig;
using VCloset.Domain.Entities;

namespace VCloset.Application.Interfaces;

public interface ITierConfigService
{
    /// <summary>Lấy toàn bộ cấu hình các tiers</summary>
    Task<IEnumerable<TierConfigResponse>> GetAllAsync();

    /// <summary>Lấy cấu hình của 1 tier cụ thể ("free" hoặc "premium")</summary>
    Task<TierConfigResponse> GetByTierAsync(string tierName);

    /// <summary>Lấy entity raw (dùng nội bộ cho service khác)</summary>
    Task<SubscriptionTierConfig> GetConfigEntityAsync(string tierName);

    /// <summary>Admin cập nhật cấu hình tier</summary>
    Task<TierConfigResponse> UpdateAsync(string tierName, UpdateTierConfigRequest request, string updatedBy);
}
