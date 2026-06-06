using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VCloset.Application.DTOs.TierConfig;
using VCloset.Application.Interfaces;
using VCloset.Domain.Entities;
using VCloset.Infrastructure.Data;

namespace VCloset.Infrastructure.Services;

public class TierConfigService : ITierConfigService
{
    private readonly VClosetVersion30Context _context;

    public TierConfigService(VClosetVersion30Context context)
    {
        _context = context;
    }

    public async Task<IEnumerable<TierConfigResponse>> GetAllAsync()
    {
        var configs = await _context.SubscriptionTierConfigs.OrderBy(c => c.InternalId).ToListAsync();
        return configs.Select(ToResponse);
    }

    public async Task<TierConfigResponse> GetByTierAsync(string tierName)
    {
        var config = await GetConfigEntityAsync(tierName);
        return ToResponse(config);
    }

    public async Task<SubscriptionTierConfig> GetConfigEntityAsync(string tierName)
    {
        var config = await _context.SubscriptionTierConfigs
            .FirstOrDefaultAsync(c => c.TierName == tierName.ToLower());

        if (config == null)
            throw new Exception($"Không tìm thấy cấu hình cho tier '{tierName}'.");

        return config;
    }

    public async Task<TierConfigResponse> UpdateAsync(string tierName, UpdateTierConfigRequest request, string updatedBy)
    {
        if (request.BgRemovalCredits < 0 || request.TryOnCredits < 0)
            throw new ArgumentException("Số lượt không được âm.");
        if (request.WardrobeItemLimit.HasValue && request.WardrobeItemLimit < 0)
            throw new ArgumentException("Giới hạn tủ đồ không được âm.");
        if (request.OutfitLimit.HasValue && request.OutfitLimit < 0)
            throw new ArgumentException("Giới hạn phối đồ không được âm.");

        var config = await GetConfigEntityAsync(tierName);
        config.BgRemovalCredits   = request.BgRemovalCredits;
        config.TryOnCredits        = request.TryOnCredits;
        config.WardrobeItemLimit   = request.WardrobeItemLimit;
        config.OutfitLimit         = request.OutfitLimit;
        config.UpdatedAt           = DateTime.UtcNow;
        config.UpdatedBy           = updatedBy;

        // Sync credit updates to all active users on this tier
        var now = DateTime.UtcNow;
        var normalizedTier = tierName.ToLower().Trim();
        if (normalizedTier == "premium")
        {
            var activePremiumUserIdsQuery = _context.PremiumSubscriptions
                .Where(ps => ps.IsActive && (!ps.ExpiresAt.HasValue || ps.ExpiresAt > now))
                .Select(ps => ps.UserInternalId)
                .Distinct();

            await _context.CustomerProfiles
                .Where(cp => activePremiumUserIdsQuery.Contains(cp.UserInternalId))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(cp => cp.BgRemovalCredits, request.BgRemovalCredits)
                    .SetProperty(cp => cp.TryOnCredits, request.TryOnCredits)
                    .SetProperty(cp => cp.UpdatedAt, now));
        }
        else if (normalizedTier == "free")
        {
            var activePremiumUserIdsQuery = _context.PremiumSubscriptions
                .Where(ps => ps.IsActive && (!ps.ExpiresAt.HasValue || ps.ExpiresAt > now))
                .Select(ps => ps.UserInternalId)
                .Distinct();

            await _context.CustomerProfiles
                .Where(cp => !activePremiumUserIdsQuery.Contains(cp.UserInternalId))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(cp => cp.BgRemovalCredits, request.BgRemovalCredits)
                    .SetProperty(cp => cp.TryOnCredits, request.TryOnCredits)
                    .SetProperty(cp => cp.UpdatedAt, now));
        }

        await _context.SaveChangesAsync();
        return ToResponse(config);
    }

    private static TierConfigResponse ToResponse(SubscriptionTierConfig c) => new()
    {
        TierName            = c.TierName,
        BgRemovalCredits    = c.BgRemovalCredits,
        TryOnCredits        = c.TryOnCredits,
        WardrobeItemLimit   = c.WardrobeItemLimit,
        OutfitLimit         = c.OutfitLimit,
        UpdatedAt           = c.UpdatedAt,
        UpdatedBy           = c.UpdatedBy
    };
}
