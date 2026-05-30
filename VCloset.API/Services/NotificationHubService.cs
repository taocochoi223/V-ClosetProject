using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using VCloset.Application.Interfaces;
using VCloset.Application.DTOs;
using VCloset.API.Hubs;

namespace VCloset.API.Services;

public class NotificationHubService : INotificationHubService
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public NotificationHubService(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task SendUnreadCountAlertAsync(int userId, int unreadCount)
    {
        // Gửi thông tin số lượng tin chưa đọc mới xuống nhóm của User đó qua SignalR
        await _hubContext.Clients.Group($"UserGroup_{userId}").SendAsync("ReceiveUnreadCount", unreadCount);
    }

    public async Task SendNotificationAlertAsync(int userId, NotificationResponseDto notification)
    {
        // Gửi toàn bộ đối tượng thông báo mới xuống nhóm của User đó qua SignalR
        await _hubContext.Clients.Group($"UserGroup_{userId}").SendAsync("ReceiveNotification", notification);
    }
}
