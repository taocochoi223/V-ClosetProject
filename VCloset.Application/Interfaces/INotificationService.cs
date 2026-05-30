using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VCloset.Application.DTOs;
using VCloset.Application.DTOs.Admin.Requests;

namespace VCloset.Application.Interfaces;

public interface INotificationService
{
    // For System/Internal use (Group C) - triggers creation of a new notification record in DB
    Task<NotificationResponseDto> SendNotificationAsync(int userId, string type, string title, string body, string? referenceType, int? referenceId);
    Task SendBroadcastNotificationAsync(string type, string title, string body, string? referenceType, int? referenceId);
    Task SaveDeviceTokenAsync(int userId, SaveDeviceTokenRequest request);
    Task<bool> BulkDeleteNotificationsAsync(int userId, List<Guid> notificationIds);

    // For Client use (Group A API operations)
    Task<List<NotificationResponseDto>> GetUserNotificationsAsync(int userId, bool? isRead, int page = 1, int pageSize = 20);
    Task<int> GetUnreadCountAsync(int userId);
    Task<bool> MarkAsReadAsync(int userId, Guid notificationId);
    Task<bool> MarkAllAsReadAsync(int userId);
    Task<bool> DeleteNotificationAsync(int userId, Guid notificationId);

    // For Admin use
    Task<List<NotificationResponseDto>> GetAllNotificationsForAdminAsync(int? targetUserId, string? type, int page = 1, int pageSize = 15);
    Task<bool> DeleteNotificationByAdminAsync(Guid notificationId);
}
