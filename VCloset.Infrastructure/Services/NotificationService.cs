using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VCloset.Application.DTOs;
using VCloset.Application.DTOs.Admin.Requests;
using VCloset.Application.Interfaces;
using VCloset.Domain.Entities;
using VCloset.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace VCloset.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationHubService _hubService;
    private readonly IEmailService _emailService;
    private readonly Microsoft.Extensions.DependencyInjection.IServiceScopeFactory _scopeFactory;

    public NotificationService(
        IUnitOfWork unitOfWork, 
        INotificationHubService hubService, 
        IEmailService emailService,
        Microsoft.Extensions.DependencyInjection.IServiceScopeFactory scopeFactory)
    {
        _unitOfWork = unitOfWork;
        _hubService = hubService;
        _emailService = emailService;
        _scopeFactory = scopeFactory;
    }

    public async Task<NotificationResponseDto> SendNotificationAsync(int userId, string type, string title, string body, string? referenceType, int? referenceId, bool sendViaApp = true, bool sendViaEmail = false)
    {
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserInternalId = userId,
            Type = type,
            Title = title,
            Body = body,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Notifications.AddAsync(notification);
        await _unitOfWork.SaveChangesAsync();

        var dto = MapToDto(notification);

        // Real-time Push Alert: Gửi số lượng tin chưa đọc mới VÀ nguyên đối tượng DTO qua SignalR
        var newCount = await GetUnreadCountAsync(userId);
        await _hubService.SendUnreadCountAlertAsync(userId, newCount);
        await _hubService.SendNotificationAlertAsync(userId, dto);

        // Đẩy thông báo qua Firebase (FCM)
        try
        {
            var userTokens = await _unitOfWork.UserDeviceTokens.FindAllAsync(t => t.UserInternalId == userId);
            var tokens = userTokens.Select(t => t.FcmToken).ToList();
            if (tokens.Any())
            {
                await PushFirebaseNotificationAsync(tokens, title, body, type, referenceType, referenceId);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WARNING] Failed to push Firebase notification to user {userId}: {ex.Message}");
        }

        if (sendViaEmail)
        {
            try
            {
                var user = await _unitOfWork.Users.FindAsync(u => u.InternalId == userId);
                if (user != null && !string.IsNullOrEmpty(user.Email))
                {
                    _ = Task.Run(async () =>
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var scopedEmailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                        try
                        {
                            await scopedEmailService.SendSystemNotificationEmailAsync(user.Email, user.DisplayName, title, body);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[WARNING] Failed to send targeted email to {user.Email}: {ex.Message}");
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WARNING] Failed to send email notification to user {userId}: {ex.Message}");
            }
        }

        return dto;
    }

    public async Task<List<NotificationResponseDto>> GetUserNotificationsAsync(int userId, bool? isRead, int page = 1, int pageSize = 20)
    {
        var notifications = await _unitOfWork.Notifications.FindAllAsync(n => n.UserInternalId == userId);
        
        var query = notifications.AsQueryable();

        if (isRead.HasValue)
        {
            query = query.Where(n => n.IsRead == isRead.Value);
        }

        var sortedNotifications = query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return sortedNotifications.Select(MapToDto).ToList();
    }

    public async Task<int> GetUnreadCountAsync(int userId)
    {
        var unread = await _unitOfWork.Notifications.FindAllAsync(n => n.UserInternalId == userId && !n.IsRead);
        return unread.Count();
    }

    public async Task<bool> MarkAsReadAsync(int userId, Guid notificationId)
    {
        var notification = await _unitOfWork.Notifications.FindAsync(n => n.Id == notificationId && n.UserInternalId == userId);

        if (notification == null) return false;

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            _unitOfWork.Notifications.Update(notification);
            await _unitOfWork.SaveChangesAsync();

            // Real-time Push Alert: Cập nhật giảm số lượng tin chưa đọc
            var newCount = await GetUnreadCountAsync(userId);
            await _hubService.SendUnreadCountAlertAsync(userId, newCount);
        }

        return true;
    }

    public async Task<bool> MarkAllAsReadAsync(int userId)
    {
        var unreadNotifications = await _unitOfWork.Notifications.FindAllAsync(n => n.UserInternalId == userId && !n.IsRead);

        if (!unreadNotifications.Any()) return true;

        foreach (var notification in unreadNotifications)
        {
            notification.IsRead = true;
            _unitOfWork.Notifications.Update(notification);
        }

        await _unitOfWork.SaveChangesAsync();

        // Real-time Push Alert: Gửi số lượng tin chưa đọc mới (= 0)
        await _hubService.SendUnreadCountAlertAsync(userId, 0);

        return true;
    }

    public async Task<bool> DeleteNotificationAsync(int userId, Guid notificationId)
    {
        var notification = await _unitOfWork.Notifications.FindAsync(n => n.Id == notificationId && n.UserInternalId == userId);

        if (notification == null) return false;

        bool wasUnread = !notification.IsRead;

        _unitOfWork.Notifications.Delete(notification);
        await _unitOfWork.SaveChangesAsync();

        // Real-time Push Alert: Cập nhật lại số lượng nếu xóa tin chưa đọc
        if (wasUnread)
        {
            var newCount = await GetUnreadCountAsync(userId);
            await _hubService.SendUnreadCountAlertAsync(userId, newCount);
        }

        return true;
    }

    public async Task SendBroadcastNotificationAsync(string type, string title, string body, string? referenceType, int? referenceId, bool sendViaApp = true, bool sendViaEmail = false)
    {
        var users = await _unitOfWork.Users.FindAllAsync(u => u.Role == UserRole.Customer && u.IsActive);

        if (!users.Any()) return;

        var notifications = new List<Notification>();
        foreach (var user in users)
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserInternalId = user.InternalId,
                Type = type,
                Title = title,
                Body = body,
                ReferenceType = referenceType,
                ReferenceId = referenceId,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.Notifications.AddAsync(notification);
            notifications.Add(notification);
        }

        await _unitOfWork.SaveChangesAsync();

        foreach (var notification in notifications)
        {
            try
            {
                var dto = MapToDto(notification);
                var newCount = await GetUnreadCountAsync(notification.UserInternalId);
                await _hubService.SendUnreadCountAlertAsync(notification.UserInternalId, newCount);
                await _hubService.SendNotificationAlertAsync(notification.UserInternalId, dto);
            }
            catch
            {
                // Bỏ qua lỗi gửi SignalR đơn lẻ nếu user offline hoặc ngắt kết nối
            }
        }

        // Đẩy thông báo qua Firebase (FCM) cho toàn bộ người nhận
        try
        {
            var userIds = users.Select(u => u.InternalId).ToList();
            var tokensList = await _unitOfWork.UserDeviceTokens.FindAllAsync(t => userIds.Contains(t.UserInternalId));
            var tokens = tokensList.Select(t => t.FcmToken).Distinct().ToList();
            if (tokens.Any())
            {
                await PushFirebaseNotificationAsync(tokens, title, body, type, referenceType, referenceId);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WARNING] Failed to push Firebase broadcast: {ex.Message}");
        }

        if (sendViaEmail)
        {
            // Sending emails asynchronously to avoid blocking the main thread for too long
            _ = Task.Run(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var scopedEmailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                foreach (var user in users)
                {
                    if (!string.IsNullOrEmpty(user.Email))
                    {
                        try
                        {
                            await scopedEmailService.SendSystemNotificationEmailAsync(user.Email, user.DisplayName, title, body);
                            // Simple delay to prevent SMTP throttling if using a basic provider
                            await Task.Delay(50);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[WARNING] Failed to send broadcast email to {user.Email}: {ex.Message}");
                        }
                    }
                }
            });
        }
    }

    public async Task SaveDeviceTokenAsync(int userId, SaveDeviceTokenRequest request)
    {
        var existingToken = await _unitOfWork.UserDeviceTokens.FindAsync(t => t.FcmToken == request.FcmToken);

        if (existingToken != null)
        {
            existingToken.UserInternalId = userId;
            existingToken.DeviceType = request.DeviceType;
            existingToken.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.UserDeviceTokens.Update(existingToken);
        }
        else
        {
            var newToken = new UserDeviceToken
            {
                Id = Guid.NewGuid(),
                UserInternalId = userId,
                FcmToken = request.FcmToken,
                DeviceType = request.DeviceType,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _unitOfWork.UserDeviceTokens.AddAsync(newToken);
        }

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<bool> BulkDeleteNotificationsAsync(int userId, List<Guid> notificationIds)
    {
        var toDelete = await _unitOfWork.Notifications.FindAllAsync(n => n.UserInternalId == userId && notificationIds.Contains(n.Id));

        if (!toDelete.Any()) return false;

        bool hasUnreadDeleted = false;

        foreach (var notification in toDelete)
        {
            if (!notification.IsRead)
            {
                hasUnreadDeleted = true;
            }
            _unitOfWork.Notifications.Delete(notification);
        }

        await _unitOfWork.SaveChangesAsync();

        if (hasUnreadDeleted)
        {
            var newCount = await GetUnreadCountAsync(userId);
            await _hubService.SendUnreadCountAlertAsync(userId, newCount);
        }

        return true;
    }

    public async Task<List<NotificationResponseDto>> GetAllNotificationsForAdminAsync(int? targetUserId, string? type, int page = 1, int pageSize = 15)
    {
        var allNotifications = await _unitOfWork.Notifications.GetAllAsync();
        var query = allNotifications.AsQueryable();

        if (targetUserId.HasValue)
        {
            query = query.Where(n => n.UserInternalId == targetUserId.Value);
        }

        if (!string.IsNullOrEmpty(type))
        {
            query = query.Where(n => n.Type.Equals(type, StringComparison.OrdinalIgnoreCase));
        }

        var sortedNotifications = query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return sortedNotifications.Select(MapToDto).ToList();
    }

    public async Task<bool> DeleteNotificationByAdminAsync(Guid notificationId)
    {
        var notification = await _unitOfWork.Notifications.FindAsync(n => n.Id == notificationId);
        if (notification == null) return false;

        bool wasUnread = !notification.IsRead;
        int userId = notification.UserInternalId;

        _unitOfWork.Notifications.Delete(notification);
        await _unitOfWork.SaveChangesAsync();

        if (wasUnread)
        {
            try
            {
                var newCount = await GetUnreadCountAsync(userId);
                await _hubService.SendUnreadCountAlertAsync(userId, newCount);
            }
            catch
            {
                // Bỏ qua lỗi gửi SignalR đơn lẻ nếu user offline hoặc ngắt kết nối
            }
        }

        return true;
    }

    private async Task PushFirebaseNotificationAsync(List<string> tokens, string title, string body, string type, string? referenceType, int? referenceId)
    {
        if (FirebaseAdmin.FirebaseApp.DefaultInstance == null || tokens == null || !tokens.Any())
        {
            return;
        }

        try
        {
            // Firebase limits multicast messages to 500 tokens per send
            for (int i = 0; i < tokens.Count; i += 500)
            {
                var batch = tokens.Skip(i).Take(500).ToList();
                var message = new FirebaseAdmin.Messaging.MulticastMessage()
                {
                    Tokens = batch,
                    Notification = new FirebaseAdmin.Messaging.Notification()
                    {
                        Title = title,
                        Body = body
                    },
                    Data = new Dictionary<string, string>()
                    {
                        { "type", type },
                        { "referenceType", referenceType ?? "" },
                        { "referenceId", referenceId?.ToString() ?? "" }
                    }
                };

                var response = await FirebaseAdmin.Messaging.FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(message);

                if (response.FailureCount > 0)
                {
                    var failedTokens = new List<string>();
                    for (int j = 0; j < response.Responses.Count; j++)
                    {
                        if (!response.Responses[j].IsSuccess)
                        {
                            var error = response.Responses[j].Exception?.MessagingErrorCode;
                            if (error == FirebaseAdmin.Messaging.MessagingErrorCode.Unregistered || 
                                error == FirebaseAdmin.Messaging.MessagingErrorCode.InvalidArgument)
                            {
                                failedTokens.Add(batch[j]);
                            }
                        }
                    }

                    if (failedTokens.Any())
                    {
                        foreach (var token in failedTokens)
                        {
                            var tokenEntity = await _unitOfWork.UserDeviceTokens.FindAsync(t => t.FcmToken == token);
                            if (tokenEntity != null)
                            {
                                _unitOfWork.UserDeviceTokens.Delete(tokenEntity);
                            }
                        }
                        await _unitOfWork.SaveChangesAsync();
                        Console.WriteLine($"[INFO] Cleared {failedTokens.Count} stale/invalid FCM device tokens from database.");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Failed to send Firebase notifications: {ex.Message}");
        }
    }

    private static NotificationResponseDto MapToDto(Notification entity)
    {
        return new NotificationResponseDto
        {
            Id = entity.Id,
            Type = entity.Type,
            Title = entity.Title,
            Body = entity.Body,
            ReferenceType = entity.ReferenceType,
            ReferenceId = entity.ReferenceId,
            IsRead = entity.IsRead,
            UserInternalId = entity.UserInternalId,
            CreatedAt = entity.CreatedAt
        };
    }
}
