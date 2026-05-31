using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VCloset.Application.DTOs.Subscriptions.Requests;
using VCloset.Application.DTOs.Subscriptions.Responses;

namespace VCloset.Application.Interfaces;

public interface IAdminSubscriptionService
{
    Task<IEnumerable<SubscriptionPlanResponse>> GetAllPlansAsync();
    Task<SubscriptionPlanResponse> GetPlanByIdAsync(Guid planId);
    Task<SubscriptionPlanResponse> CreatePlanAsync(CreateOrUpdatePlanRequest request);
    Task<SubscriptionPlanResponse> UpdatePlanAsync(Guid planId, UpdatePlanRequest request);
    Task<bool> DeletePlanAsync(Guid planId);
}
